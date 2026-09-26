using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs.Operations;
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

    // ── POST /api/operations/reorder-route-optimization ──────
    /// <summary>Non-CRUD: solves TSP route logic to recalculate stops on disruption.</summary>
    [HttpPost("reorder-route-optimization")]
    public async Task<IActionResult> ReorderRouteOptimization(
        [FromBody] RouteOptimizationRequestDto dto)
    {
        var result = await _service.ReorderRouteOptimizationAsync(dto);
        return Ok(result);
    }

    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard([FromQuery] string email)
    {
        var user = _db.Users.FirstOrDefault(u => u.Email == email);
        if (user == null) return NotFound();

        bool isGuide = user.Role == "Local Guide" || user.Role == "Tour Operator";

        var result = new
        {
            role = user.Role,
            headerName = isGuide ? user.FullName : "Ketut Alit",
            headerTitle = isGuide ? "LOCAL GUIDE" : "ASSIGNED GUIDE",
            stats = isGuide ? new[]
            {
                new { value = "2", label = "TOURS TODAY" },
                new { value = "14", label = "TRAVELERS" },
                new { value = "4.9", label = "MY RATING" }
            } : new[]
            {
                new { value = "1", label = "UPCOMING TOUR" },
                new { value = "3", label = "COMPLETED" },
                new { value = "4.9", label = "GUIDE RATING" }
            },
            tours = new[]
            {
                new 
                {
                    time = "09:00 AM - 12:00 PM",
                    travelers = isGuide ? "6 Travelers" : "You + 5 others",
                    title = "Sacred Ubud Forest Walk",
                    location = "Ubud Monkey Forest Main Entrance",
                    actionType = "CHECK_IN"
                },
                new
                {
                    time = "04:30 PM - 07:30 PM",
                    travelers = isGuide ? "8 Travelers" : "You + 7 others",
                    title = "Sunset Tanah Lot Escape",
                    location = "Tanah Lot Temple Lobby",
                    actionType = "START_TOUR"
                }
            }
        };

        return Ok(result);
    }
}
