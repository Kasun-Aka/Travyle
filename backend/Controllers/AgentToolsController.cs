using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Services.Agent;

namespace Travyle.Api.Controllers;

public record CheckCapacityToolRequest(Guid ScheduleId, DateTime Date, string TimeSlot, int Guests);
public record CalculateSummaryToolRequest(Guid ScheduleId, int Guests);

/// <summary>
/// Controlled internal API endpoints for the Smart Booking Agent (FastAPI).
/// Dispatches directly to IBookingAgentTools and PostgreSQL without exposing arbitrary SQL or raw DB access.
/// </summary>
[ApiController]
[Route("api/agent/tools")]
[Produces("application/json")]
public class AgentToolsController : ControllerBase
{
    private readonly IBookingAgentTools _tools;
    private readonly TravyleDbContext _db;
    private readonly IConfiguration _configuration;

    public AgentToolsController(IBookingAgentTools tools, TravyleDbContext db, IConfiguration configuration)
    {
        _tools = tools;
        _db = db;
        _configuration = configuration;
    }

    /// <summary>
    /// Check whether traveler exists in Travyle database.
    /// </summary>
    [HttpGet("traveler/{travelerId:guid}")]
    public async Task<IActionResult> CheckTraveler(Guid travelerId, CancellationToken ct)
    {
        var traveler = await _db.Users.FirstOrDefaultAsync(u => u.Id == travelerId, ct);
        if (traveler == null)
        {
            return NotFound(new { exists = false, message = "Traveler not found" });
        }
        return Ok(new
        {
            exists = true,
            id = traveler.Id,
            fullName = traveler.FullName,
            email = traveler.Email
        });
    }

    /// <summary>
    /// Tool 1: get_available_booking_schedules
    /// </summary>
    [HttpGet("schedules")]
    public async Task<IActionResult> GetSchedules(
        [FromQuery] string? query,
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        var schedules = await _tools.GetAvailableBookingSchedulesAsync(query, date, ct);
        return Ok(schedules);
    }

    /// <summary>
    /// Tool 2: get_booking_schedule_details
    /// </summary>
    [HttpGet("schedules/{id:guid}")]
    public async Task<IActionResult> GetScheduleDetails(Guid id, CancellationToken ct)
    {
        var schedule = await _tools.GetBookingScheduleDetailsAsync(id, ct);
        return schedule == null ? NotFound(new { error = $"Schedule {id} not found." }) : Ok(schedule);
    }

    /// <summary>
    /// Tool 3: check_booking_capacity
    /// </summary>
    [HttpPost("check-capacity")]
    public async Task<IActionResult> CheckCapacity(
        [FromBody] CheckCapacityToolRequest request,
        CancellationToken ct)
    {
        var result = await _tools.CheckBookingCapacityAsync(
            request.ScheduleId,
            request.Date,
            request.TimeSlot,
            request.Guests,
            ct);
        return Ok(result);
    }

    /// <summary>
    /// Tool 4: get_traveler_bookings
    /// </summary>
    [HttpGet("traveler-bookings/{travelerId:guid}")]
    public async Task<IActionResult> GetTravelerBookings(Guid travelerId, CancellationToken ct)
    {
        var bookings = await _tools.GetTravelerBookingsAsync(travelerId, ct);
        return Ok(bookings);
    }

    /// <summary>
    /// Tool 5: calculate_booking_summary
    /// </summary>
    [HttpPost("calculate-summary")]
    public async Task<IActionResult> CalculateSummary(
        [FromBody] CalculateSummaryToolRequest request,
        CancellationToken ct)
    {
        var summary = await _tools.CalculateBookingSummaryAsync(request.ScheduleId, request.Guests, ct);
        return summary == null
            ? NotFound(new { error = "Unable to calculate booking summary for the specified schedule." })
            : Ok(summary);
    }

    /// <summary>
    /// Tool 6: create_booking
    /// </summary>
    [HttpPost("create-booking")]
    public async Task<IActionResult> CreateBooking(
        [FromBody] CreateBookingRequest request,
        CancellationToken ct)
    {
        var expectedKey = _configuration["AgentTools:ApiKey"] ?? Environment.GetEnvironmentVariable("AGENT_TOOLS_API_KEY");
        var suppliedKey = Request.Headers["X-Agent-Tools-Key"].FirstOrDefault();
        if (string.IsNullOrWhiteSpace(expectedKey) || !string.Equals(suppliedKey, expectedKey, StringComparison.Ordinal))
        {
            return Unauthorized(new { error = "Booking creation is only available to the trusted agent execution path." });
        }

        var (booking, error) = await _tools.CreateBookingAsync(request, ct);
        if (error != null)
        {
            return BadRequest(new { error });
        }
        return Ok(booking);
    }

    /// <summary>
    /// Tool 7: get_booking_status
    /// </summary>
    [HttpGet("bookings/{id:guid}")]
    public async Task<IActionResult> GetBookingStatus(Guid id, CancellationToken ct)
    {
        var booking = await _tools.GetBookingStatusAsync(id, ct);
        return booking == null ? NotFound(new { error = $"Booking {id} not found." }) : Ok(booking);
    }
}
