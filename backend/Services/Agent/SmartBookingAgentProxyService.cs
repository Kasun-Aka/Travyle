using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services.Agent;

namespace Travyle.Api.Services.Agent;

public class SmartBookingAgentProxyService : ISmartBookingAgentService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IBookingAgentTools _tools;
    private readonly TravyleDbContext _db;
    private SmartBookingAgentService? _fallbackService;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public SmartBookingAgentProxyService(IHttpClientFactory httpClientFactory, IBookingAgentTools tools, TravyleDbContext db)
    {
        _httpClientFactory = httpClientFactory;
        _tools = tools;
        _db = db;
    }

    private SmartBookingAgentService FallbackService => _fallbackService ??= new SmartBookingAgentService(_db, _tools);

    public async Task<AgentWorkflowResponse> StartWorkflowAsync(StartAgentBookingRequest request, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient("SmartBookingAgent");
        var json = JsonSerializer.Serialize(request, JsonOpts);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        AgentWorkflowResponse? workflow = null;

        try
        {
            var response = await client.PostAsync("/agent/booking/run", content, ct);
            if (response.IsSuccessStatusCode)
            {
                var responseJson = await response.Content.ReadAsStringAsync(ct);
                workflow = JsonSerializer.Deserialize<AgentWorkflowResponse>(responseJson, JsonOpts);
            }
        }
        catch
        {
            // Python service temporarily unreachable - graceful fallback to internal service
            return await FallbackService.StartWorkflowAsync(request, ct);
        }

        if (workflow == null)
        {
            return await FallbackService.StartWorkflowAsync(request, ct);
        }

        // Persist the workflow in PostgreSQL
        var exists = await _db.BookingAgentWorkflows.AnyAsync(w => w.Id == workflow.Id, ct);
        if (!exists)
        {
            var entity = new BookingAgentWorkflow
            {
                Id = workflow.Id,
                TravelerId = workflow.TravelerId,
                TravelerName = workflow.TravelerName,
                TravelerEmail = workflow.TravelerEmail,
                Objective = workflow.Objective,
                Status = Enum.TryParse<AgentWorkflowStatus>(workflow.Status, true, out var parsedStatus)
                    ? parsedStatus
                    : AgentWorkflowStatus.Running,
                PlanJson = JsonSerializer.Serialize(workflow.Plan, JsonOpts),
                CompletedStepsJson = JsonSerializer.Serialize(workflow.CompletedSteps, JsonOpts),
                ToolResultsJson = JsonSerializer.Serialize(workflow.ToolResults, JsonOpts),
                ValidationResultsJson = JsonSerializer.Serialize(workflow.ValidationResults, JsonOpts),
                ProposedBookingJson = workflow.ProposedBooking != null ? JsonSerializer.Serialize(workflow.ProposedBooking, JsonOpts) : null,
                CreatedBookingId = workflow.CreatedBookingId,
                BookingReference = workflow.BookingReference,
                ApprovalStatus = workflow.ApprovalStatus,
                ApprovedBy = workflow.ApprovedBy,
                ApproverRole = workflow.ApproverRole,
                ApproverNotes = workflow.ApproverNotes,
                ApprovedAt = workflow.ApprovedAt,
                ErrorMessage = workflow.ErrorMessage,
                CreatedAt = workflow.CreatedAt,
                UpdatedAt = workflow.UpdatedAt
            };
            _db.BookingAgentWorkflows.Add(entity);
            await _db.SaveChangesAsync(ct);
        }

        return workflow;
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

        // Role check: Only Admin or Operator allowed
        var approverRole = request.ApproverRole?.Trim();
        var isAuthorized = string.Equals(approverRole, "Admin", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(approverRole, "Operator", StringComparison.OrdinalIgnoreCase);

        if (!isAuthorized)
        {
            return (null, "Unauthorized: Only users with 'Admin' or 'Operator' role can approve agent workflows.");
        }

        // Try executing through Python agent execute endpoint
        var client = _httpClientFactory.CreateClient("SmartBookingAgent");
        try
        {
            var execPayload = new
            {
                workflow = new
                {
                    workflow_id = wf.Id,
                    traveler_id = wf.TravelerId,
                    traveler_name = wf.TravelerName,
                    traveler_email = wf.TravelerEmail,
                    proposed_booking = string.IsNullOrEmpty(wf.ProposedBookingJson) ? null : JsonSerializer.Deserialize<ProposedBookingDto>(wf.ProposedBookingJson, JsonOpts)
                },
                approverRole = request.ApproverRole,
                approverUserId = request.ApproverUserId,
                approverNotes = request.ApproverNotes
            };

            var postContent = new StringContent(JsonSerializer.Serialize(execPayload, JsonOpts), Encoding.UTF8, "application/json");
            var response = await client.PostAsync("/agent/booking/execute", postContent, ct);

            if (response.IsSuccessStatusCode)
            {
                var respStr = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(respStr);
                var root = doc.RootElement;
                if (root.TryGetProperty("created_booking_id", out var bookingIdElem) &&
                    Guid.TryParse(bookingIdElem.GetString(), out var createdId))
                {
                    wf.CreatedBookingId = createdId;
                    wf.BookingReference = root.TryGetProperty("booking_reference", out var refElem) ? refElem.GetString() : null;
                    wf.Status = AgentWorkflowStatus.Completed;
                    wf.ApprovalStatus = "APPROVED";
                    wf.ApprovedBy = request.ApproverUserId ?? "Admin";
                    wf.ApproverRole = request.ApproverRole;
                    wf.ApproverNotes = request.ApproverNotes;
                    wf.ApprovedAt = DateTime.UtcNow;
                    wf.UpdatedAt = DateTime.UtcNow;

                    var steps = JsonSerializer.Deserialize<List<string>>(wf.CompletedStepsJson, JsonOpts) ?? new List<string>();
                    if (root.TryGetProperty("completed_step_approval", out var stepAppr))
                        steps.Add(stepAppr.GetString() ?? "");
                    if (root.TryGetProperty("completed_step_execution", out var stepExec))
                        steps.Add(stepExec.GetString() ?? "");
                    wf.CompletedStepsJson = JsonSerializer.Serialize(steps, JsonOpts);

                    await _db.SaveChangesAsync(ct);
                    return (ToResponse(wf), null);
                }
            }
        }
        catch
        {
            // If python agent call failed, fall back to robust internal C# execution
        }

        return await FallbackService.ApproveWorkflowAsync(workflowId, request, ct);
    }

    public Task<(AgentWorkflowResponse? Workflow, string? Error)> RejectWorkflowAsync(
        Guid workflowId,
        RejectWorkflowRequest request,
        CancellationToken ct = default)
    {
        return FallbackService.RejectWorkflowAsync(workflowId, request, ct);
    }

    public Task<AgentWorkflowResponse?> GetWorkflowByIdAsync(Guid workflowId, CancellationToken ct = default)
    {
        return FallbackService.GetWorkflowByIdAsync(workflowId, ct);
    }

    public Task<IEnumerable<AgentWorkflowResponse>> GetTravelerWorkflowsAsync(Guid travelerId, CancellationToken ct = default)
    {
        return FallbackService.GetTravelerWorkflowsAsync(travelerId, ct);
    }

    public Task<IEnumerable<AgentWorkflowResponse>> GetPendingWorkflowsAsync(CancellationToken ct = default)
    {
        return FallbackService.GetPendingWorkflowsAsync(ct);
    }

    private static AgentWorkflowResponse ToResponse(BookingAgentWorkflow wf)
    {
        var plan = JsonSerializer.Deserialize<List<string>>(wf.PlanJson, JsonOpts) ?? new List<string>();
        var completedSteps = JsonSerializer.Deserialize<List<string>>(wf.CompletedStepsJson, JsonOpts) ?? new List<string>();
        var toolResults = JsonSerializer.Deserialize<Dictionary<string, object?>>(wf.ToolResultsJson, JsonOpts) ?? new Dictionary<string, object?>();
        var validationResults = JsonSerializer.Deserialize<Dictionary<string, bool>>(wf.ValidationResultsJson, JsonOpts) ?? new Dictionary<string, bool>();
        var proposed = string.IsNullOrWhiteSpace(wf.ProposedBookingJson)
            ? null
            : JsonSerializer.Deserialize<ProposedBookingDto>(wf.ProposedBookingJson, JsonOpts);

        return new AgentWorkflowResponse(
            wf.Id,
            wf.TravelerId,
            wf.TravelerName,
            wf.TravelerEmail,
            wf.Objective,
            wf.Status.ToString(),
            plan,
            completedSteps,
            toolResults,
            validationResults,
            proposed,
            wf.CreatedBookingId,
            wf.BookingReference,
            wf.ApprovalStatus,
            wf.ApprovedBy,
            wf.ApproverRole,
            wf.ApproverNotes,
            wf.ApprovedAt,
            wf.ErrorMessage,
            wf.CreatedAt,
            wf.UpdatedAt
        );
    }
}
