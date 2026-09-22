using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Repositories;

public class BookingScheduleRepository : IBookingScheduleRepository
{
    private readonly TravyleDbContext _db;

    public BookingScheduleRepository(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<BookingSchedule>> GetAllAsync(string? search, DateTime? filterDate, CancellationToken ct = default)
    {
        var query = _db.BookingSchedules
            .Include(s => s.AvailableDates)
            .Include(s => s.TimeSlots)
            .Where(s => s.IsActive)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var lower = search.ToLower();
            query = query.Where(s =>
                s.DestinationTitle.ToLower().Contains(lower) ||
                s.Location.ToLower().Contains(lower) ||
                s.GuideName.ToLower().Contains(lower));
        }

        if (filterDate.HasValue)
        {
            var date = filterDate.Value.Date;
            query = query.Where(s => s.AvailableDates.Any(d => d.Date.Date == date));
        }

        return await query.ToListAsync(ct);
    }

    public async Task<BookingSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.BookingSchedules
            .Include(s => s.AvailableDates)
            .Include(s => s.TimeSlots)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    public async Task<BookingSchedule> CreateAsync(BookingSchedule schedule, CancellationToken ct = default)
    {
        _db.BookingSchedules.Add(schedule);
        await _db.SaveChangesAsync(ct);
        return schedule;
    }

    public async Task<BookingSchedule?> UpdateAsync(BookingSchedule schedule, CancellationToken ct = default)
    {
        var existing = await _db.BookingSchedules
            .Include(s => s.AvailableDates)
            .Include(s => s.TimeSlots)
            .FirstOrDefaultAsync(s => s.Id == schedule.Id, ct);
        if (existing == null) return null;

        existing.DestinationId = schedule.DestinationId;
        existing.DestinationTitle = schedule.DestinationTitle;
        existing.Location = schedule.Location;
        existing.GuideName = schedule.GuideName;
        existing.PricePerPerson = schedule.PricePerPerson;
        existing.MaxCapacityPerSlot = schedule.MaxCapacityPerSlot;
        existing.Rating = schedule.Rating;
        existing.ReviewsCount = schedule.ReviewsCount;
        existing.AvailableDates = schedule.AvailableDates;
        existing.TimeSlots = schedule.TimeSlots;
        await _db.SaveChangesAsync(ct);
        return await GetByIdAsync(existing.Id, ct);
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var existing = await _db.BookingSchedules.FindAsync(new object[] { id }, ct);
        if (existing == null) return false;

        existing.IsActive = false;
        await _db.SaveChangesAsync(ct);
        return true;
    }

    public async Task<Dictionary<string, int>> GetBookedSlotsMapAsync(Guid scheduleId, CancellationToken ct = default)
    {
        // Returns a map of "YYYY-MM-DD_HH:MM AM/PM" -> booked count
        var bookings = await _db.Bookings
            .Where(b => b.ScheduleId == scheduleId &&
                        b.Status != BookingStatus.Cancelled)
            .ToListAsync(ct);

        return bookings
            .GroupBy(b => $"{b.BookingDate:yyyy-MM-dd}_{b.TimeSlot}")
            .ToDictionary(g => g.Key, g => g.Sum(b => b.Guests));
    }
}

public class BookingRepository : IBookingRepository
{
    private readonly TravyleDbContext _db;

    public BookingRepository(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<Booking>> GetAllAsync(int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Bookings
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<Booking>> GetByTravelerIdAsync(Guid travelerId, int page, int pageSize, CancellationToken ct = default)
    {
        return await _db.Bookings
            .Where(b => b.TravelerId == travelerId)
            .OrderByDescending(b => b.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);
    }

    public async Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Bookings
            .Include(b => b.PaymentEscrow)
            .Include(b => b.DiscountRequest)
            .FirstOrDefaultAsync(b => b.Id == id, ct);
    }

    public async Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default)
    {
        _db.Bookings.Add(booking);
        await _db.SaveChangesAsync(ct);
        return booking;
    }

    public async Task<Booking?> UpdateAsync(Booking booking, CancellationToken ct = default)
    {
        var existing = await _db.Bookings.FindAsync(new object[] { booking.Id }, ct);
        if (existing == null) return null;

        existing.Status = booking.Status;
        existing.PaymentStatus = booking.PaymentStatus;
        existing.PaymentMethod = booking.PaymentMethod;
        existing.DiscountAmount = booking.DiscountAmount;
        existing.TotalAmount = booking.TotalAmount;
        existing.Notes = booking.Notes;
        existing.TransactionRef = booking.TransactionRef;
        existing.ReceiptReference = booking.ReceiptReference;
        existing.ReceiptImageData = booking.ReceiptImageData;
        existing.EscrowReleaseDate = booking.EscrowReleaseDate;
        existing.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return existing;
    }

    public async Task<bool> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await _db.Bookings.FindAsync(new object[] { id }, ct);
        if (booking == null) return false;

        // Soft cancel instead of hard delete
        booking.Status = BookingStatus.Cancelled;
        if (booking.PaymentStatus == EscrowStatus.HeldInEscrow)
            booking.PaymentStatus = EscrowStatus.Refunded;
        booking.UpdatedAt = DateTime.UtcNow;

        await _db.SaveChangesAsync(ct);
        return true;
    }
}

public class DiscountRequestRepository : IDiscountRequestRepository
{
    private readonly TravyleDbContext _db;

    public DiscountRequestRepository(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<DiscountRequest>> GetAllAsync(CancellationToken ct = default)
    {
        return await _db.DiscountRequests
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<IEnumerable<DiscountRequest>> GetByTravelerIdAsync(Guid travelerId, CancellationToken ct = default)
    {
        return await _db.DiscountRequests
            .Where(d => d.TravelerId == travelerId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<DiscountRequest?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.DiscountRequests.FirstOrDefaultAsync(d => d.Id == id, ct);
    }

    public async Task<DiscountRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.DiscountRequests.FirstOrDefaultAsync(d => d.BookingId == bookingId, ct);
    }

    public async Task<DiscountRequest> CreateAsync(DiscountRequest request, CancellationToken ct = default)
    {
        _db.DiscountRequests.Add(request);
        await _db.SaveChangesAsync(ct);
        return request;
    }

    public async Task<DiscountRequest?> UpdateAsync(DiscountRequest request, CancellationToken ct = default)
    {
        var existing = await _db.DiscountRequests.FindAsync(new object[] { request.Id }, ct);
        if (existing == null) return null;

        existing.Status = request.Status;
        existing.UpdatedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return existing;
    }
}

public class PaymentEscrowRepository : IPaymentEscrowRepository
{
    private readonly TravyleDbContext _db;

    public PaymentEscrowRepository(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<PaymentEscrow?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default)
    {
        return await _db.PaymentEscrows.FirstOrDefaultAsync(e => e.BookingId == bookingId, ct);
    }

    public async Task<PaymentEscrow> CreateAsync(PaymentEscrow escrow, CancellationToken ct = default)
    {
        _db.PaymentEscrows.Add(escrow);
        await _db.SaveChangesAsync(ct);
        return escrow;
    }

    public async Task<PaymentEscrow?> UpdateAsync(PaymentEscrow escrow, CancellationToken ct = default)
    {
        var existing = await _db.PaymentEscrows.FindAsync(new object[] { escrow.Id }, ct);
        if (existing == null) return null;

        existing.Status = escrow.Status;
        existing.Amount = escrow.Amount;
        existing.RefundedAmount = escrow.RefundedAmount;
        existing.ReleasedAt = escrow.ReleasedAt;
        await _db.SaveChangesAsync(ct);
        return existing;
    }
}
