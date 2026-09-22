using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

/// <summary>
/// POST   /api/bookings                         – create a booking
/// GET    /api/bookings/traveler/{travelerId}   – list a traveler's bookings (paginated)
/// GET    /api/bookings/{id}                    – get booking detail
/// PUT    /api/bookings/{id}/status             – update booking status
/// DELETE /api/bookings/{id}                    – cancel booking
/// POST   /api/bookings/{id}/process-escrow-payment – initiate escrow payment
/// </summary>
[ApiController]
[Route("api/bookings")]
[Produces("application/json")]
public class BookingsController : ControllerBase
{
    private readonly IBookingService _bookingService;
    private readonly IPaymentEscrowService _escrowService;

    public BookingsController(IBookingService bookingService, IPaymentEscrowService escrowService)
    {
        _bookingService = bookingService;
        _escrowService = escrowService;
    }

    /// <summary>
    /// Returns a paginated list of bookings for a specific traveler.
    /// Query params: page (default 1), pageSize (default 20).
    /// </summary>
    [HttpGet("traveler/{travelerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTravelerBookings(
        Guid travelerId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _bookingService.GetTravelerBookingsAsync(travelerId, page, pageSize, ct);
        return Ok(result);
    }

    [HttpGet("admin/all")]
    [ProducesResponseType(typeof(IEnumerable<BookingResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAllForAdmin(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        return Ok(await _bookingService.GetAllBookingsAsync(page, pageSize, ct));
    }

    /// <summary>
    /// Returns a single booking by its ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _bookingService.GetBookingByIdAsync(id, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Creates a new booking. Validates slot capacity server-side.
    /// Returns 422 with an error message if the slot is full.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateBookingRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (booking, error) = await _bookingService.CreateBookingAsync(request, ct);
        if (error != null)
            return UnprocessableEntity(new { error });

        return CreatedAtAction(nameof(GetById), new { id = booking!.Id }, booking);
    }

    /// <summary>
    /// Updates the status of an existing booking.
    /// Accepted status values: Pending, Confirmed, Completed, Cancelled.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var result = await _bookingService.UpdateBookingStatusAsync(id, request.Status, ct);
        if (result == null) return NotFound(new { error = $"Booking {id} not found or status '{request.Status}' is invalid." });

        return Ok(result);
    }

    /// <summary>
    /// Cancels a booking (soft delete: status set to Cancelled, escrow to Refunded if applicable).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var success = await _bookingService.CancelBookingAsync(id, ct);
        return success ? NoContent() : NotFound();
    }

    /// <summary>
    /// Processes an escrow payment for a booking.
    /// Sets booking status to Confirmed and payment status to HeldInEscrow.
    /// </summary>
    [HttpPost("{id:guid}/process-escrow-payment")]
    [ProducesResponseType(typeof(PaymentEscrowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ProcessEscrowPayment(Guid id, [FromBody] ProcessEscrowPaymentRequest request, CancellationToken ct)
    {
        if (id != request.BookingId)
            return BadRequest(new { error = "Route id and request BookingId must match." });

        var (result, error) = await _escrowService.ProcessEscrowPaymentAsync(request, ct);
        if (error != null)
            return UnprocessableEntity(new { error });

        return Ok(result);
    }
}
