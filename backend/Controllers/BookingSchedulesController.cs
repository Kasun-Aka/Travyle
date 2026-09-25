using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

/// <summary>
/// GET /api/booking-schedules        – list all (with optional search + date filter)
/// GET /api/booking-schedules/{id}   – get one schedule
/// POST /api/booking-schedules       – create a schedule (admin / guide)
/// PUT /api/booking-schedules/{id}   – edit a schedule (admin / guide)
/// DELETE /api/booking-schedules/{id} – delete an unbooked upcoming schedule
/// </summary>
[ApiController]
[Route("api/booking-schedules")]
[Produces("application/json")]
public class BookingSchedulesController : ControllerBase
{
    private readonly IBookingScheduleService _service;

    public BookingSchedulesController(IBookingScheduleService service)
    {
        _service = service;
    }

    /// <summary>
    /// Returns all active booking schedules.
    /// Supports optional query params: search (string), date (ISO-8601 date string).
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<BookingScheduleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] string? search,
        [FromQuery] DateTime? date,
        CancellationToken ct)
    {
        var result = await _service.GetSchedulesAsync(search, date, ct);
        return Ok(result);
    }

    /// <summary>
    /// Returns a single booking schedule by its ID.
    /// Also includes the live bookedSlotsMap so the Flutter client can check capacity.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _service.GetScheduleByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new booking schedule.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingScheduleResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateBookingScheduleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _service.CreateScheduleAsync(request, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(BookingScheduleResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBookingScheduleRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (result, error) = await _service.UpdateScheduleAsync(id, request, ct);
        if (result == null) return BadRequest(new { error });
        return Ok(result);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        var (success, error) = await _service.DeleteScheduleAsync(id, ct);
        if (!success && error == "Schedule not found.") return NotFound(new { error });
        if (!success) return BadRequest(new { error });
        return NoContent();
    }

    [HttpPut("{id:guid}/slots")]
    public async Task<IActionResult> UpdateSlot(Guid id, [FromBody] UpdateScheduleSlotRequest request, CancellationToken ct)
    {
        var (result, error) = await _service.UpdateSlotAsync(id, request, ct);
        return result == null ? BadRequest(new { error }) : Ok(result);
    }

    [HttpDelete("{id:guid}/slots")]
    public async Task<IActionResult> DeleteSlot(Guid id, [FromQuery] DateTime date, [FromQuery] string timeSlot, CancellationToken ct)
    {
        var (result, error) = await _service.DeleteSlotAsync(id, date, timeSlot, ct);
        return result == null ? BadRequest(new { error }) : Ok(result);
    }
}
