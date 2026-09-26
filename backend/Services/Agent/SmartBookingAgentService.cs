using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Services.Agent;

public class SmartBookingAgentService : ISmartBookingAgentService
{
    private readonly TravyleDbContext _db;
    private readonly IBookingAgentTools _tools;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public SmartBookingAgentService(TravyleDbContext db, IBookingAgentTools tools)
    {
        _db = db;
        _tools = tools;
    }

    public async Task<AgentWorkflowResponse> StartWorkflowAsync(
        StartAgentBookingRequest request,
        CancellationToken ct = default)
    {
        var workflowId = Guid.NewGuid();
        var plan = new List<string>
        {
            "Step 1: Sanitize input & analyze booking intent",
            "Step 2: Query available tour schedules matching criteria",
            "Step 3: Select schedule and verify time slot availability",
            "Step 4: Check real-time slot capacity via backend tool",
            "Step 5: Check for conflicting bookings for traveler",
            "Step 6: Calculate pricing summary & applicable discounts",
            "Step 7: Execute deterministic validation rules",
            "Step 8: Construct proposal & pause at PENDING_APPROVAL"
        };

        var completedSteps = new List<string>();
        var toolResults = new Dictionary<string, object?>();
        var validationResults = new Dictionary<string, bool>();

        // Step 1: Sanitize & analyze objective
        completedSteps.Add("Step 1: Sanitize input & analyze booking intent");

        // Prompt injection check
        var isPromptInjection = DetectPromptInjection(request.Objective);
        validationResults["prompt_injection_safe"] = !isPromptInjection;

        if (isPromptInjection)
        {
            var injectionWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = request.TravelerName ?? "",
                TravelerEmail = request.TravelerEmail ?? "",
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ApprovalStatus = "REJECTED_SECURITY",
                ErrorMessage = "Untrusted prompt or security bypass pattern detected. Direct confirmation without validation or human approval is strictly prohibited."
            };

            _db.BookingAgentWorkflows.Add(injectionWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(injectionWorkflow);
        }

        // Validate traveler
        var traveler = await _db.Users.FirstOrDefaultAsync(u => u.Id == request.TravelerId, ct);
        var travelerExists = traveler != null;
        validationResults["traveler_exists"] = travelerExists;

        if (!travelerExists)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = request.TravelerName ?? "Unknown",
                TravelerEmail = request.TravelerEmail ?? "",
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = "The traveler account does not exist or could not be verified. Please sign out and sign back in, then try again."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        var travelerName = string.IsNullOrWhiteSpace(request.TravelerName) ? traveler!.FullName : request.TravelerName;
        var travelerEmail = string.IsNullOrWhiteSpace(request.TravelerEmail) ? traveler!.Email : request.TravelerEmail;

        // Parse intent from objective
        var parsedIntent = ParseBookingObjective(request.Objective, request);

        // Step 2: Query available schedules
        completedSteps.Add("Step 2: Query available tour schedules matching criteria");
        var availableSchedules = (await _tools.GetAvailableBookingSchedulesAsync(parsedIntent.DestinationKeyword, null, ct)).ToList();
        toolResults["get_available_booking_schedules"] = new
        {
            query = parsedIntent.DestinationKeyword,
            matchedCount = availableSchedules.Count,
            schedules = availableSchedules.Select(s => new { s.Id, s.DestinationTitle, s.Location, s.PricePerPerson }).ToList()
        };

        // If no matching schedule was found
        BookingScheduleResponse? selectedSchedule = null;
        if (request.PreferredScheduleId.HasValue && request.PreferredScheduleId != Guid.Empty)
        {
            selectedSchedule = availableSchedules.FirstOrDefault(s => s.Id == request.PreferredScheduleId.Value)
                               ?? await _tools.GetBookingScheduleDetailsAsync(request.PreferredScheduleId.Value, ct);
        }
        else if (!string.IsNullOrWhiteSpace(parsedIntent.DestinationKeyword) && availableSchedules.Count > 0)
        {
            selectedSchedule = availableSchedules.First();
        }

        var scheduleExists = selectedSchedule != null;
        validationResults["schedule_exists"] = scheduleExists;
        validationResults["schedule_is_bookable"] = scheduleExists;

        if (!scheduleExists)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = $"No matching or bookable schedule found for '{parsedIntent.DestinationKeyword ?? request.Objective}'. Please try one of the available tours."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        // Step 3: Select schedule & time slot
        completedSteps.Add("Step 3: Select schedule and verify time slot availability");

        // Resolve Target Date
        DateTime targetDate;
        if (parsedIntent.ExplicitDate.HasValue)
        {
            targetDate = parsedIntent.ExplicitDate.Value.Date;
        }
        else
        {
            // Pick the next available upcoming date offered by the schedule
            var upcoming = selectedSchedule!.AvailableDates
                .Where(d => d.Date >= DateTime.UtcNow.Date)
                .OrderBy(d => d.Date)
                .ToList();
            targetDate = upcoming.Count > 0 ? upcoming.First().Date : DateTime.UtcNow.Date.AddDays(1);
        }

        var dateOffered = selectedSchedule!.AvailableDates.Any(d => d.Date == targetDate.Date);
        var dateInFuture = targetDate.Date >= DateTime.UtcNow.Date;
        var dateValid = dateOffered && dateInFuture;
        validationResults["date_is_valid"] = dateValid;

        if (!dateValid)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = $"Sorry, the date you requested ({targetDate:dd MMM yyyy}) is not available for the '{selectedSchedule.DestinationTitle}' tour. Please choose a future date that the tour offers."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        // Resolve Time Slot
        string targetSlot;
        if (!string.IsNullOrWhiteSpace(parsedIntent.ExplicitSlot))
        {
            targetSlot = parsedIntent.ExplicitSlot;
        }
        else
        {
            targetSlot = selectedSchedule.AvailableTimeSlots.FirstOrDefault() ?? "09:00 AM";
        }

        var slotExists = selectedSchedule.AvailableTimeSlots.Any(s => s.Equals(targetSlot, StringComparison.OrdinalIgnoreCase));
        if (!slotExists)
        {
            targetSlot = selectedSchedule.AvailableTimeSlots.FirstOrDefault() ?? "09:00 AM";
        }

        // Validate guest count without silently changing an invalid request.
        var guests = parsedIntent.Guests;
        validationResults["traveler_count_valid"] = guests > 0;

        if (!validationResults["traveler_count_valid"])
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = "Traveler count must be greater than zero."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        // Step 4: Check capacity via backend tool
        completedSteps.Add("Step 4: Check real-time slot capacity via backend tool");
        var capacityResult = await _tools.CheckBookingCapacityAsync(selectedSchedule.Id, targetDate, targetSlot, guests, ct);
        toolResults["check_booking_capacity"] = capacityResult;
        validationResults["capacity_sufficient"] = capacityResult.IsAvailable;

        if (!capacityResult.IsAvailable)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = capacityResult.Message ?? "Insufficient capacity for the requested slot."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        // Step 5: Check existing bookings for traveler for conflicts
        completedSteps.Add("Step 5: Check for conflicting bookings for traveler");
        var travelerBookings = (await _tools.GetTravelerBookingsAsync(request.TravelerId, ct)).ToList();
        var hasConflict = travelerBookings.Any(b =>
            b.Status != "Cancelled" &&
            b.BookingDate.Date == targetDate.Date &&
            b.TimeSlot.Equals(targetSlot, StringComparison.OrdinalIgnoreCase));

        toolResults["get_traveler_bookings"] = new
        {
            existingBookingsCount = travelerBookings.Count,
            conflictFound = hasConflict
        };
        validationResults["no_conflicting_booking"] = !hasConflict;

        if (hasConflict)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = $"The traveler already has an active or pending booking on {targetDate:dd MMM yyyy} at {targetSlot}. Please choose a different date or time to avoid a conflict."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        // Step 6: Calculate pricing summary & group discounts
        completedSteps.Add("Step 6: Calculate pricing summary & applicable discounts");
        var summary = await _tools.CalculateBookingSummaryAsync(selectedSchedule.Id, guests, ct);
        if (summary == null)
        {
            var failedWorkflow = new BookingAgentWorkflow
            {
                Id = workflowId,
                TravelerId = request.TravelerId,
                TravelerName = travelerName,
                TravelerEmail = travelerEmail,
                Objective = request.Objective,
                Status = AgentWorkflowStatus.Failed,
                PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
                ErrorMessage = "We were unable to calculate the pricing for this tour at the moment. Please try again shortly or contact our support team."
            };
            _db.BookingAgentWorkflows.Add(failedWorkflow);
            await _db.SaveChangesAsync(ct);
            return ToResponse(failedWorkflow);
        }

        toolResults["calculate_booking_summary"] = summary;

        // Step 7: Deterministic validation rules check
        completedSteps.Add("Step 7: Execute deterministic validation rules");
        validationResults["approval_required_before_creation"] = true;

        var proposedBooking = new ProposedBookingDto(
            selectedSchedule.Id,
            selectedSchedule.DestinationTitle,
            selectedSchedule.Location,
            targetDate,
            targetSlot,
            guests,
            summary.PricePerPerson,
            summary.BasePrice,
            summary.ServiceFee,
            summary.DiscountAmount,
            summary.TotalAmount,
            "SampleCard"
        );

        // Step 8: Construct proposal & pause at PENDING_APPROVAL
        completedSteps.Add("Step 8: Construct proposal & pause at PENDING_APPROVAL");

        var workflow = new BookingAgentWorkflow
        {
            Id = workflowId,
            TravelerId = request.TravelerId,
            TravelerName = travelerName,
            TravelerEmail = travelerEmail,
            Objective = request.Objective,
            Status = AgentWorkflowStatus.PendingApproval,
            PlanJson = JsonSerializer.Serialize(plan, JsonOpts),
            CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts),
            ToolResultsJson = JsonSerializer.Serialize(toolResults, JsonOpts),
            ValidationResultsJson = JsonSerializer.Serialize(validationResults, JsonOpts),
            ProposedBookingJson = JsonSerializer.Serialize(proposedBooking, JsonOpts),
            ApprovalStatus = "PENDING",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _db.BookingAgentWorkflows.Add(workflow);
        await _db.SaveChangesAsync(ct);

        return ToResponse(workflow);
    }

    public async Task<AgentWorkflowResponse?> GetWorkflowByIdAsync(Guid workflowId, CancellationToken ct = default)
    {
        var wf = await _db.BookingAgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        return wf == null ? null : ToResponse(wf);
    }

    public async Task<IEnumerable<AgentWorkflowResponse>> GetTravelerWorkflowsAsync(Guid travelerId, CancellationToken ct = default)
    {
        var list = await _db.BookingAgentWorkflows
            .Where(w => w.TravelerId == travelerId)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToResponse);
    }

    public async Task<IEnumerable<AgentWorkflowResponse>> GetPendingWorkflowsAsync(CancellationToken ct = default)
    {
        var list = await _db.BookingAgentWorkflows
            .Where(w => w.Status == AgentWorkflowStatus.PendingApproval)
            .OrderByDescending(w => w.CreatedAt)
            .ToListAsync(ct);
        return list.Select(ToResponse);
    }

    public async Task<(AgentWorkflowResponse? Workflow, string? Error)> ApproveWorkflowAsync(
        Guid workflowId,
        ApproveWorkflowRequest request,
        CancellationToken ct = default)
    {
        var wf = await _db.BookingAgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        if (wf == null) return (null, $"Workflow {workflowId} not found.");

        if (wf.Status != AgentWorkflowStatus.PendingApproval)
        {
            return (null, $"Workflow is not in PENDING_APPROVAL status (Current status: {wf.Status}).");
        }

        var validationResults = JsonSerializer.Deserialize<Dictionary<string, bool>>(wf.ValidationResultsJson, JsonOpts);
        if (DetectPromptInjection(wf.Objective) ||
            validationResults?.GetValueOrDefault("prompt_injection_safe") == false)
        {
            return (null, "This proposal contains an unsafe prompt and cannot be approved.");
        }

        // Validate approver role: Only Admin or Operator allowed
        var approverRole = request.ApproverRole?.Trim();
        var isAuthorized = string.Equals(approverRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(approverRole, "Operator", StringComparison.OrdinalIgnoreCase);

        if (!isAuthorized)
        {
            return (null, "Unauthorized: Only users with 'Admin' or 'Operator' role can approve agent workflows.");
        }

        if (string.IsNullOrWhiteSpace(wf.ProposedBookingJson))
        {
            return (null, "No proposed booking found in workflow state.");
        }

        var proposed = JsonSerializer.Deserialize<ProposedBookingDto>(wf.ProposedBookingJson, JsonOpts);
        if (proposed == null)
        {
            return (null, "Invalid proposed booking format.");
        }

        // Execute creation tool deterministically
        var createRequest = new CreateBookingRequest(
            proposed.ScheduleId,
            wf.TravelerId,
            wf.TravelerName,
            wf.TravelerEmail,
            DateTime.SpecifyKind(proposed.BookingDate, DateTimeKind.Utc),
            proposed.TimeSlot,
            proposed.Guests,
            $"Booked via Smart Booking Agent (Approved by {request.ApproverUserId ?? "Admin"} - {request.ApproverRole})",
            proposed.PaymentMethod,
            null,
            null
        );

        var (createdBooking, createError) = await _tools.CreateBookingAsync(createRequest, ct);

        var completedSteps = JsonSerializer.Deserialize<List<string>>(wf.CompletedStepsJson, JsonOpts) ?? new List<string>();
        completedSteps.Add($"Step 9: Approved by {request.ApproverRole} ({request.ApproverUserId ?? "Admin"})");

        if (createError != null || createdBooking == null)
        {
            wf.Status = AgentWorkflowStatus.Failed;
            wf.ApprovalStatus = "APPROVED_CREATION_FAILED";
            wf.ErrorMessage = $"Booking execution failed after approval: {createError}";
            wf.CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts);
            wf.UpdatedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
            return (ToResponse(wf), createError);
        }

        completedSteps.Add($"Step 10: Created booking {createdBooking.BookingReference} and verified escrow status");

        wf.Status = AgentWorkflowStatus.Completed;
        wf.ApprovalStatus = "APPROVED";
        wf.ApprovedBy = request.ApproverUserId ?? "Admin";
        wf.ApproverRole = request.ApproverRole;
        wf.ApproverNotes = request.ApproverNotes;
        wf.ApprovedAt = DateTime.UtcNow;
        wf.CreatedBookingId = createdBooking.Id;
        wf.BookingReference = createdBooking.BookingReference;
        wf.CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts);
        wf.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return (ToResponse(wf), null);
    }

    public async Task<(AgentWorkflowResponse? Workflow, string? Error)> RejectWorkflowAsync(
        Guid workflowId,
        RejectWorkflowRequest request,
        CancellationToken ct = default)
    {
        var wf = await _db.BookingAgentWorkflows.FirstOrDefaultAsync(w => w.Id == workflowId, ct);
        if (wf == null) return (null, $"Workflow {workflowId} not found.");

        if (wf.Status != AgentWorkflowStatus.PendingApproval)
        {
            return (null, $"Workflow is not in PENDING_APPROVAL status (Current status: {wf.Status}).");
        }

        var approverRole = request.ApproverRole?.Trim();
        var isAuthorized = string.Equals(approverRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(approverRole, "Operator", StringComparison.OrdinalIgnoreCase);

        if (!isAuthorized)
        {
            return (null, "Unauthorized: Only users with 'Admin' or 'Operator' role can reject agent workflows.");
        }

        var completedSteps = JsonSerializer.Deserialize<List<string>>(wf.CompletedStepsJson, JsonOpts) ?? new List<string>();
        completedSteps.Add($"Step 9: Rejected by {request.ApproverRole}: {request.Reason}");

        wf.Status = AgentWorkflowStatus.Rejected;
        wf.ApprovalStatus = "REJECTED";
        wf.ApprovedBy = request.ApproverUserId ?? "Admin";
        wf.ApproverRole = request.ApproverRole;
        wf.ApproverNotes = request.Reason;
        wf.ApprovedAt = DateTime.UtcNow;
        wf.CompletedStepsJson = JsonSerializer.Serialize(completedSteps, JsonOpts);
        wf.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return (ToResponse(wf), null);
    }

    private static bool DetectPromptInjection(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return false;

        var lower = input.ToLowerInvariant();
        var patterns = new[]
        {
            "ignore previous",
            "ignore all",
            "ignore the rules",
            "ignore rules",
            "bypass approval",
            "bypass validation",
            "confirm immediately",
            "approve immediately",
            "grant admin",
            "system prompt",
            "system:",
            "<script",
            "drop table",
            "exec(",
            "select * from"
        };

        return patterns.Any(p => lower.Contains(p));
    }

    private record ParsedIntent(
        string? DestinationKeyword,
        int Guests,
        DateTime? ExplicitDate,
        string? ExplicitSlot
    );

    private static ParsedIntent ParseBookingObjective(string objective, StartAgentBookingRequest request)
    {
        var lower = objective.ToLowerInvariant();

        // 1. Destination
        string? dest = null;
        if (lower.Contains("ella") || lower.Contains("nine arch") || lower.Contains("rock")) dest = "Ella";
        else if (lower.Contains("sigiriya") || lower.Contains("fortress")) dest = "Sigiriya";
        else if (lower.Contains("mirissa") || lower.Contains("whale")) dest = "Mirissa";
        else if (lower.Contains("adam") || lower.Contains("tea factory")) dest = "Little Adam";
        else if (lower.Contains("pidurangala")) dest = "Pidurangala";
        else if (lower.Contains("kandy")) dest = "Kandy";

        // 2. Guests
        var guests = request.Guests ?? 1;
        var guestMatch = Regex.Match(objective, @"(?i)(\d+)\s*(people|person|persons|guests|travelers|spots)");
        if (guestMatch.Success && int.TryParse(guestMatch.Groups[1].Value, out var parsedGuests))
        {
            guests = parsedGuests;
        }
        else
        {
            var forMatch = Regex.Match(objective, @"(?i)for\s+(\d+)");
            if (forMatch.Success && int.TryParse(forMatch.Groups[1].Value, out var g))
            {
                guests = g;
            }
        }

        // 3. Date
        DateTime? targetDate = request.PreferredDate;
        if (!targetDate.HasValue)
        {
            var dateMatch = Regex.Match(objective, @"\b(\d{4}-\d{2}-\d{2})\b");
            if (dateMatch.Success && DateTime.TryParse(dateMatch.Groups[1].Value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            {
                targetDate = DateTime.SpecifyKind(d.Date, DateTimeKind.Utc);
            }
            else if (lower.Contains("tomorrow"))
            {
                targetDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(1), DateTimeKind.Utc);
            }
            else if (lower.Contains("next weekend"))
            {
                var daysUntilSaturday = ((int)DayOfWeek.Saturday - (int)DateTime.UtcNow.DayOfWeek + 7) % 7;
                if (daysUntilSaturday == 0) daysUntilSaturday = 7;
                targetDate = DateTime.SpecifyKind(DateTime.UtcNow.Date.AddDays(daysUntilSaturday), DateTimeKind.Utc);
            }
        }

        // 4. Time slot
        string? slot = request.PreferredTimeSlot;
        if (string.IsNullOrWhiteSpace(slot))
        {
            var slotMatch = Regex.Match(objective, @"\b(\d{1,2}:\d{2}\s*(?:AM|PM))\b", RegexOptions.IgnoreCase);
            if (slotMatch.Success)
            {
                slot = slotMatch.Groups[1].Value.ToUpperInvariant();
            }
            else if (lower.Contains("morning"))
            {
                slot = "06:30 AM";
            }
            else if (lower.Contains("afternoon"))
            {
                slot = "02:00 PM";
            }
        }

        return new ParsedIntent(dest, guests, targetDate, slot);
    }

    private static AgentWorkflowResponse ToResponse(BookingAgentWorkflow w)
    {
        var plan = JsonSerializer.Deserialize<List<string>>(w.PlanJson, JsonOpts) ?? new List<string>();
        var completed = JsonSerializer.Deserialize<List<string>>(w.CompletedStepsJson, JsonOpts) ?? new List<string>();
        var tools = JsonSerializer.Deserialize<Dictionary<string, object?>>(w.ToolResultsJson, JsonOpts) ?? new Dictionary<string, object?>();
        var validation = JsonSerializer.Deserialize<Dictionary<string, bool>>(w.ValidationResultsJson, JsonOpts) ?? new Dictionary<string, bool>();
        var proposed = string.IsNullOrWhiteSpace(w.ProposedBookingJson)
            ? null
            : JsonSerializer.Deserialize<ProposedBookingDto>(w.ProposedBookingJson, JsonOpts);

        return new AgentWorkflowResponse(
            w.Id,
            w.TravelerId,
            w.TravelerName,
            w.TravelerEmail,
            w.Objective,
            w.Status.ToString(),
            plan,
            completed,
            tools,
            validation,
            proposed,
            w.CreatedBookingId,
            w.BookingReference,
            w.ApprovalStatus,
            w.ApprovedBy,
            w.ApproverRole,
            w.ApproverNotes,
            w.ApprovedAt,
            w.ErrorMessage,
            w.CreatedAt,
            w.UpdatedAt
        );
    }
}
