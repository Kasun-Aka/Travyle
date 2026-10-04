using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.DTOs.Operations;
using Travyle.Api.Models;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/operations")]
public class OperationsController : ControllerBase
{
    private readonly IOperationsService _service;
    private readonly Travyle.Api.Data.TravyleDbContext _db;

    public OperationsController(IOperationsService service, Travyle.Api.Data.TravyleDbContext db)
    {
        _service = service;
        _db = db;
    }

    // ── POST /api/operations/activities ──────────────────────
    /// <summary>Log daily tour activity schedules.</summary>
    [HttpPost("activities")]
    public async Task<IActionResult> CreateTourActivity([FromBody] CreateTourActivityDto dto)
    {
        var result = await _service.CreateTourActivityAsync(dto);
        return CreatedAtAction(nameof(CreateTourActivity), new { id = result.Id }, result);
    }

    // ── GET /api/operations/guides/available ─────────────────
    /// <summary>Filter &amp; paginate verified guides by location/language.</summary>
    [HttpGet("guides/available")]
    public async Task<IActionResult> GetAvailableGuides(
        [FromQuery] string? location,
        [FromQuery] string? language,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20)
    {
        var result = await _service.GetAvailableGuidesAsync(location, language, page, pageSize);
        return Ok(result);
    }

    // ── PUT /api/operations/assignments/{id} ─────────────────
    /// <summary>Update assigned guide or driver.</summary>
    [HttpPut("assignments/{id:guid}")]
    public async Task<IActionResult> UpdateGuideAssignment(
        Guid id, [FromBody] UpdateGuideAssignmentDto dto)
    {
        var result = await _service.UpdateGuideAssignmentAsync(id, dto);
        if (result is null) return NotFound();
        return Ok(result);
    }

    // ── GET /api/operations/routes/{tourId} ──────────────────
    /// <summary>Retrieve real-time active tour progress (GPS pings).</summary>
    [HttpGet("routes/{tourId:guid}")]
    public async Task<IActionResult> GetRoutesByTourId(Guid tourId)
    {
        var result = await _service.GetRouteLogsByTourIdAsync(tourId);
        return Ok(result);
    }

    // ── POST /api/operations/route-logs ──────────────────────
    /// <summary>Log a GPS ping from the guide app.</summary>
    [HttpPost("route-logs")]
    public async Task<IActionResult> CreateRouteLog([FromBody] CreateRouteLogDto dto)
    {
        var result = await _service.CreateRouteLogAsync(dto);
        return CreatedAtAction(nameof(CreateRouteLog), new { id = result.Id }, result);
    }

    // ── GET /api/operations/disruption-alerts ────────────────
    /// <summary>List active weather/traffic alerts for a tour.</summary>
    [HttpGet("disruption-alerts")]
    public async Task<IActionResult> GetDisruptionAlerts(
        [FromQuery] Guid? bookingScheduleId)
    {
        var result = await _service.GetActiveAlertsAsync(bookingScheduleId);
        return Ok(result);
    }

    [HttpPost("disruption-alerts")]
    public async Task<IActionResult> CreateDisruptionAlert([FromBody] CreateDisruptionAlertDto dto)
    {
        var result = await _service.CreateDisruptionAlertAsync(dto);
        return CreatedAtAction(nameof(CreateDisruptionAlert), new { id = result.Id }, result);
    }

    // ── POST /api/operations/reorder-route-optimization ──────
    /// <summary>Non-CRUD: solves TSP route logic to recalculate stops on disruption.</summary>
    [HttpPost("reorder-route-optimization")]
    public async Task<IActionResult> ReorderRouteOptimization(
        [FromBody] RouteOptimizationRequestDto dto)
    {
        var result = await _service.ReorderRouteOptimizationAsync(dto);
        return Ok(result);
    }

    // ── POST /api/operations/monitor ─────────────────────────
    /// <summary>Proxy to Python AI Agent for operation monitoring (weather/traffic).</summary>
    [HttpPost("monitor")]
    public async Task<IActionResult> MonitorOperations([FromBody] MonitorOperationsRequestDto dto)
    {
        try
        {
            var result = await _service.MonitorOperationsAsync(dto);
            return Ok(result);
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = "Failed to communicate with AI Agent", details = ex.Message });
        }
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string email)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user == null) return NotFound();

        bool isGuide = user.Role == "Local Guide" || user.Role == "Tour Operator";

        var realAssignedSchedules = new List<BookingSchedule>();
        GuideAssignment? primaryGuideAssignment = null;

        if (isGuide)
        {
            var assignedScheduleIds = await _db.GuideAssignments
                .Where(ga => ga.GuideUserId == user.Id)
                .Select(ga => ga.BookingScheduleId)
                .ToListAsync();

            realAssignedSchedules = await _db.BookingSchedules
                .Include(s => s.TimeSlots)
                .Include(s => s.AvailableDates)
                .Where(s => assignedScheduleIds.Contains(s.Id) || s.GuideName.ToLower() == user.FullName.ToLower())
                .ToListAsync();
        }
        else
        {
            // Tourist: Get their bookings
            var bookingScheduleIds = await _db.Bookings
                .Where(b => b.TravelerId == user.Id || b.TravelerEmail.ToLower() == email.ToLower())
                .Select(b => b.ScheduleId)
                .Distinct()
                .ToListAsync();

            realAssignedSchedules = await _db.BookingSchedules
                .Include(s => s.TimeSlots)
                .Include(s => s.AvailableDates)
                .Where(s => bookingScheduleIds.Contains(s.Id))
                .ToListAsync();

            // Find the assigned guide for their first upcoming schedule
            if (realAssignedSchedules.Count > 0)
            {
                var firstScheduleId = realAssignedSchedules.First().Id;
                primaryGuideAssignment = await _db.GuideAssignments
                    .Include(ga => ga.Guide)
                    .FirstOrDefaultAsync(ga => ga.BookingScheduleId == firstScheduleId);
            }
        }

        var dynamicTours = realAssignedSchedules.Count > 0
            ? realAssignedSchedules.Select((s, idx) => new
            {
                time = s.TimeSlots.FirstOrDefault()?.SlotLabel != null
                    ? $"{s.TimeSlots.FirstOrDefault()!.SlotLabel} - Onwards"
                    : "09:00 AM - 01:00 PM",
                travelers = isGuide ? $"{s.MaxCapacityPerSlot} Max Capacity" : "You + Others",
                title = s.DestinationTitle,
                location = s.Location,
                actionType = idx == 0 ? "CHECK_IN" : "START_TOUR"
            }).ToArray()
            : Array.Empty<object>();

        var dynamicStats = isGuide
            ? (realAssignedSchedules.Count > 0 ? new[]
                {
                    new { value = realAssignedSchedules.Count.ToString(), label = "TOURS ASSIGNED" },
                    new { value = realAssignedSchedules.Sum(s => s.MaxCapacityPerSlot).ToString(), label = "CAPACITY" },
                    new { value = (realAssignedSchedules.Average(s => s.Rating) > 0 ? realAssignedSchedules.Average(s => s.Rating).ToString("0.0") : "5.0"), label = "MY RATING" }
                }
                : new[]
                {
                    new { value = "0", label = "TOURS TODAY" },
                    new { value = "0", label = "TRAVELERS" },
                    new { value = "0.0", label = "MY RATING" }
                })
            : (realAssignedSchedules.Count > 0 ? new[]
                {
                    new { value = realAssignedSchedules.Count.ToString(), label = "UPCOMING TOURS" },
                    new { value = "0", label = "COMPLETED" },
                    new { value = "4.9", label = "GUIDE RATING" }
                }
                : new[]
                {
                    new { value = "0", label = "UPCOMING TOUR" },
                    new { value = "0", label = "COMPLETED" },
                    new { value = "0.0", label = "GUIDE RATING" }
                });

        string headerName = user.FullName;
        string headerTitle = isGuide ? "LOCAL GUIDE" : "TOURIST";

        if (!isGuide && primaryGuideAssignment?.Guide != null)
        {
            headerName = primaryGuideAssignment.Guide.FullName;
            headerTitle = "ASSIGNED GUIDE";
        }

        var result = new
        {
            role = user.Role,
            headerName = headerName,
            headerTitle = headerTitle,
            stats = dynamicStats,
            tours = dynamicTours
        };

        return Ok(result);
    }
}
