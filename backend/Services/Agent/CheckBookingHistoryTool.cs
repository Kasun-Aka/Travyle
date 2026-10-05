using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Services.Agent;

public record CheckBookingHistoryInput(
    Guid UserId,
    Guid? BookingId = null,
    int LookbackDays = 365
);

public record CheckBookingHistoryOutput(
    Guid UserId,
    Guid? BookingId,
    bool BookingReferenced,
    bool BookingVerified,
    BookingStatus? BookingStatus,
    EscrowStatus? PaymentStatus,
    decimal? BookingTotalAmount,
    int TotalBookingsInLookback,
    int CompletedBookingsInLookback
);

/// <summary>
/// Component 4 agent tool: READ-ONLY verification of a customer's booking history.
/// It only reads the Bookings table (owned by the Bookings component) and never modifies it.
/// </summary>
public interface ICheckBookingHistoryTool
{
    Task<CheckBookingHistoryOutput> ExecuteAsync(CheckBookingHistoryInput input, CancellationToken cancellationToken = default);
}

public class CheckBookingHistoryTool : ICheckBookingHistoryTool
{
    private readonly TravyleDbContext _db;

    public CheckBookingHistoryTool(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<CheckBookingHistoryOutput> ExecuteAsync(CheckBookingHistoryInput input, CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            return new CheckBookingHistoryOutput(Guid.Empty, input.BookingId, input.BookingId.HasValue, false, null, null, null, 0, 0);
        }

        // Input validation (least privilege): bounded lookback window.
        var days = input.LookbackDays > 0 ? Math.Min(input.LookbackDays, 730) : 365;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var history = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.TravelerId == input.UserId && b.CreatedAt >= cutoff)
            .Select(b => new { b.Status })
            .ToListAsync(cancellationToken);

        var total = history.Count;
        var completed = history.Count(b => b.Status == BookingStatus.Completed);

        if (!input.BookingId.HasValue)
        {
            return new CheckBookingHistoryOutput(input.UserId, null, false, false, null, null, null, total, completed);
        }

        var booking = await _db.Bookings
            .AsNoTracking()
            .Where(b => b.Id == input.BookingId.Value)
            .Select(b => new { b.TravelerId, b.Status, b.PaymentStatus, b.TotalAmount })
            .FirstOrDefaultAsync(cancellationToken);

        // A booking only counts as verified when it exists AND belongs to the ticket owner.
        // Details of someone else's booking are never returned.
        if (booking == null || booking.TravelerId != input.UserId)
        {
            return new CheckBookingHistoryOutput(input.UserId, input.BookingId, true, false, null, null, null, total, completed);
        }

        return new CheckBookingHistoryOutput(
            input.UserId,
            input.BookingId,
            true,
            true,
            booking.Status,
            booking.PaymentStatus,
            booking.TotalAmount,
            total,
            completed);
    }
}
