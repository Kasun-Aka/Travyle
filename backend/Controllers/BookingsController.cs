using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services;
using Travyle.Api.Services.Auth;

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
    private readonly IFirebaseIdentityService _identityService;

    public BookingsController(
        IBookingService bookingService,
        IPaymentEscrowService escrowService,
        IFirebaseIdentityService identityService)
    {
        _bookingService = bookingService;
        _escrowService = escrowService;
        _identityService = identityService;
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
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        if (user.Id != travelerId) return Forbid();

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
        if (await _identityService.VerifyStaffAsync(Request, ct) == null)
            return Forbid();

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
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });

        var result = await _bookingService.GetBookingByIdAsync(id, ct);
        if (result == null) return NotFound();
        if (result.TravelerId != user.Id && !IsStaff(user)) return NotFound();
        return Ok(result);
    }

    /// <summary>
    /// Displays the current travel-pass details when a booking QR is scanned.
    /// </summary>
    [HttpGet("{id:guid}/pass")]
    [Produces("text/html")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status410Gone)]
    public async Task<IActionResult> GetPass(
        Guid id,
        [FromQuery] DateTime? revision,
        CancellationToken ct)
    {
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);
        if (booking == null) return NotFound();

        if (string.Equals(booking.Status, nameof(BookingStatus.Cancelled), StringComparison.OrdinalIgnoreCase))
        {
            return Content(PassPage("Booking cancelled", "This booking has been cancelled. This travel pass is no longer valid.", null), "text/html; charset=utf-8");
        }

        if (string.Equals(booking.Status, nameof(BookingStatus.Completed), StringComparison.OrdinalIgnoreCase) ||
            HasTripFinished(booking.BookingDate, booking.TimeSlot))
        {
            return Content(PassPage("Trip already finished", "This trip was already finished.", null), "text/html; charset=utf-8");
        }

        if (revision.HasValue && revision.Value.ToUniversalTime() != booking.UpdatedAt.ToUniversalTime())
        {
            Response.StatusCode = StatusCodes.Status410Gone;
            return Content(PassPage(
                "Pass replaced",
                "This QR pass was replaced after the booking was edited. Please scan the latest pass from the traveler's booking.",
                null), "text/html; charset=utf-8");
        }

                var isAgentBooking = booking.Notes?.Contains("Smart Booking Agent", StringComparison.OrdinalIgnoreCase) == true;
                var paymentDue = isAgentBooking && booking.PaymentStatus == nameof(EscrowStatus.Pending);
                var paymentText = paymentDue
                        ? "Payment due"
                        : string.Equals(booking.PaymentStatus, nameof(EscrowStatus.Paid), StringComparison.OrdinalIgnoreCase)
                                ? "Paid"
                                : WebUtility.HtmlEncode(booking.PaymentStatus);
                var amountLabel = paymentDue ? "Amount due" : "Amount paid";
                var passTitle = paymentDue ? "AI booking pass · Payment due" : "Traveler booking pass";

                var details = $"""
            <dl>
              <dt>Passenger</dt><dd>{WebUtility.HtmlEncode(booking.TravelerName)}</dd>
              <dt>Destination</dt><dd>{WebUtility.HtmlEncode(booking.DestinationTitle)}</dd>
              <dt>Location</dt><dd>{WebUtility.HtmlEncode(booking.Location)}</dd>
              <dt>Travel date</dt><dd>{booking.BookingDate.ToString("dddd, d MMMM yyyy", CultureInfo.InvariantCulture)}</dd>
              <dt>Time</dt><dd>{WebUtility.HtmlEncode(booking.TimeSlot)}</dd>
              <dt>Seats</dt><dd>{booking.Guests}</dd>
                            <dt>{amountLabel}</dt><dd>LKR {booking.TotalAmount.ToString("N2", CultureInfo.InvariantCulture)}</dd>
                            <dt>Payment</dt><dd>{paymentText}</dd>
              <dt>Status</dt><dd>{WebUtility.HtmlEncode(booking.Status)}</dd>
            </dl>
            """;
                return Content(PassPage(passTitle, details, booking.BookingReference), "text/html; charset=utf-8");
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
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        if (request.TravelerId != user.Id) return Forbid();

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
        if (await _identityService.VerifyStaffAsync(Request, ct) == null)
            return Forbid();

        var result = await _bookingService.UpdateBookingStatusAsync(id, request.Status, ct);
        if (result == null) return NotFound(new { error = $"Booking {id} not found or status '{request.Status}' is invalid." });

        return Ok(result);
    }

    /// <summary>
    /// Records manually received payment for an AI-created booking and confirms it without escrow processing.
    /// </summary>
    [HttpPost("{id:guid}/confirm-manual-payment")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> ConfirmManualPayment(Guid id, CancellationToken ct)
    {
        if (await _identityService.VerifyStaffAsync(Request, ct) == null)
            return Forbid();

        var (booking, error) = await _bookingService.ConfirmManualAgentPaymentAsync(id, ct);
        if (error != null) return UnprocessableEntity(new { error });
        return Ok(booking);
    }

    /// <summary>
    /// Updates a traveler's booking date, time slot, or notes before the trip is completed.
    /// </summary>
    [HttpPut("{id:guid}/details")]
    [ProducesResponseType(typeof(BookingResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> UpdateDetails(
        Guid id,
        [FromBody] UpdateBookingDetailsRequest request,
        CancellationToken ct)
    {
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });

        var existing = await _bookingService.GetBookingByIdAsync(id, ct);
        if (existing == null) return NotFound();
        if (existing.TravelerId != user.Id && !IsStaff(user)) return NotFound();

        var (booking, error) = await _bookingService.UpdateBookingDetailsAsync(id, request, ct);
        if (error != null) return UnprocessableEntity(new { error });
        return Ok(booking);
    }

    /// <summary>
    /// Cancels a booking (soft delete: status set to Cancelled, escrow to Refunded if applicable).
    /// </summary>
    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct)
    {
        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);
        if (booking == null) return NotFound();
        if (booking.TravelerId != user.Id && !IsStaff(user)) return NotFound();

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

        var user = await _identityService.VerifyUserAsync(Request, ct);
        if (user == null) return Unauthorized(new { error = "A valid Firebase sign-in is required." });
        var booking = await _bookingService.GetBookingByIdAsync(id, ct);
        if (booking == null) return NotFound();
        if (booking.TravelerId != user.Id && !IsStaff(user)) return NotFound();

        var (result, error) = await _escrowService.ProcessEscrowPaymentAsync(request, ct);
        if (error != null)
            return UnprocessableEntity(new { error });

        return Ok(result);
    }

    private static bool IsStaff(User user) =>
        string.Equals(user.Role, "Admin", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(user.Role, "Operator", StringComparison.OrdinalIgnoreCase);

    private static bool HasTripFinished(DateTime bookingDate, string timeSlot)
    {
        var nowInSriLanka = DateTime.UtcNow.AddHours(5.5);
        if (bookingDate.Date < nowInSriLanka.Date) return true;
        if (bookingDate.Date > nowInSriLanka.Date) return false;

        if (!DateTime.TryParseExact(
                timeSlot,
                "hh:mm tt",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var parsedTime))
            return false;

        return bookingDate.Date.Add(parsedTime.TimeOfDay) < nowInSriLanka;
    }

    private static string PassPage(string title, string content, string? reference)
    {
        var body = reference == null
            ? $"<p>{content}</p>"
            : $"<p class=\"reference\">{WebUtility.HtmlEncode(reference)}</p>{content}";
                return $$"""
            <!doctype html>
            <html lang="en">
            <head>
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta charset="utf-8">
                            <title>{{WebUtility.HtmlEncode(title)}} | Travyle</title>
              <style>
                                body{font:16px system-ui,sans-serif;background:#f2f7fa;color:#163a50;margin:0;padding:24px}
                                main{max-width:520px;margin:32px auto;background:white;border:1px solid #d9e5eb;border-radius:12px;padding:24px}
                                h1{font-size:24px;margin:0 0 18px} dl{display:grid;grid-template-columns:130px 1fr;gap:12px;margin:0}
                                dt{color:#61798a} dd{margin:0;font-weight:650;overflow-wrap:anywhere} .reference{color:#47728c}
              </style>
            </head>
                        <body><main><h1>{{WebUtility.HtmlEncode(title)}}</h1>{{body}}</main></body>
            </html>
            """;
    }
}
