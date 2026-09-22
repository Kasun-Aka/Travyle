using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Repositories;
using System.Globalization;

namespace Travyle.Api.Services;

// ─── Mappers ─────────────────────────────────────────────────────────────────

internal static class BookingMapper
{
    public static BookingScheduleResponse ToResponse(BookingSchedule s, Dictionary<string, int> bookedSlotsMap)
    {
        return new BookingScheduleResponse(
            s.Id,
            s.DestinationId,
            s.DestinationTitle,
            s.Location,
            s.GuideName,
            s.PricePerPerson,
            s.MaxCapacityPerSlot,
            s.Rating,
            s.ReviewsCount,
            s.AvailableDates.Select(d => d.Date).OrderBy(d => d).ToList(),
            s.TimeSlots.Select(t => t.SlotLabel).ToList(),
            bookedSlotsMap
        );
    }

    public static BookingResponse ToResponse(Booking b)
    {
        return new BookingResponse(
            b.Id,
            b.BookingReference,
            b.ScheduleId,
            b.DestinationTitle,
            b.Location,
            b.TravelerId,
            b.TravelerName,
            b.TravelerEmail,
            b.BookingDate,
            b.TimeSlot,
            b.Guests,
            b.BasePrice,
            b.ServiceFee,
            b.DiscountAmount,
            b.TotalAmount,
            b.Status.ToString(),
            b.PaymentStatus.ToString(),
            b.PaymentMethod.ToString(),
            b.Notes,
            b.TransactionRef,
            b.ReceiptReference,
            b.ReceiptImageData,
            b.EscrowReleaseDate,
            b.CreatedAt
        );
    }

    public static DiscountRequestResponse ToResponse(DiscountRequest d)
    {
        return new DiscountRequestResponse(
            d.Id,
            d.BookingId,
            d.ScheduleId,
            d.TravelerId,
            d.OriginalPrice,
            d.RequestedDiscountPercent,
            d.CalculatedDiscountAmount,
            d.Reason,
            d.Status.ToString(),
            d.CreatedAt
        );
    }

    public static PaymentEscrowResponse ToResponse(PaymentEscrow e)
    {
        return new PaymentEscrowResponse(
            e.Id,
            e.BookingId,
            e.Amount,
            e.Status.ToString(),
            e.TransactionRef,
            e.CreatedAt,
            e.EscrowReleaseDate,
            e.ReleasedAt
        );
    }
}

// ─── BookingScheduleService ──────────────────────────────────────────────────

public class BookingScheduleService : IBookingScheduleService
{
    private readonly IBookingScheduleRepository _scheduleRepo;
    private readonly IBookingRepository _bookingRepo;

    public BookingScheduleService(IBookingScheduleRepository scheduleRepo, IBookingRepository bookingRepo)
    {
        _scheduleRepo = scheduleRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<IEnumerable<BookingScheduleResponse>> GetSchedulesAsync(string? search, DateTime? filterDate, CancellationToken ct = default)
    {
        var schedules = await _scheduleRepo.GetAllAsync(search, filterDate, ct);
        var results = new List<BookingScheduleResponse>();

        foreach (var s in schedules)
        {
            var map = await _scheduleRepo.GetBookedSlotsMapAsync(s.Id, ct);
            results.Add(BookingMapper.ToResponse(s, map));
        }

        return results;
    }

    public async Task<BookingScheduleResponse?> GetScheduleByIdAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetByIdAsync(id, ct);
        if (schedule == null) return null;

        var map = await _scheduleRepo.GetBookedSlotsMapAsync(id, ct);
        return BookingMapper.ToResponse(schedule, map);
    }

    public async Task<BookingScheduleResponse> CreateScheduleAsync(CreateBookingScheduleRequest request, CancellationToken ct = default)
    {
        var schedule = new BookingSchedule
        {
            DestinationId = request.DestinationId,
            DestinationTitle = request.DestinationTitle,
            Location = request.Location,
            GuideName = request.GuideName,
            PricePerPerson = request.PricePerPerson,
            MaxCapacityPerSlot = request.MaxCapacityPerSlot,
            Rating = request.Rating,
            ReviewsCount = request.ReviewsCount,
            AvailableDates = request.AvailableDates
                .Select(d => new ScheduleAvailableDate { Date = d.ToUniversalTime() })
                .ToList(),
            TimeSlots = request.AvailableTimeSlots
                .Select(t => new ScheduleTimeSlot { SlotLabel = t })
                .ToList()
        };

        var created = await _scheduleRepo.CreateAsync(schedule, ct);
        return BookingMapper.ToResponse(created, new Dictionary<string, int>());
    }

    public async Task<(BookingScheduleResponse? Result, string? Error)> UpdateScheduleAsync(
        Guid id,
        UpdateBookingScheduleRequest request,
        CancellationToken ct = default)
    {
        var existing = await _scheduleRepo.GetByIdAsync(id, ct);
        if (existing == null) return (null, "Schedule not found.");

        var now = DateTime.UtcNow;
        if (request.AvailableDates.Count == 0 || request.AvailableTimeSlots.Count == 0)
            return (null, "At least one date and time slot are required.");
        if (request.MaxCapacityPerSlot < 1 || request.PricePerPerson < 0)
            return (null, "Capacity and price must be valid values.");
        if (request.AvailableDates.Any(date => request.AvailableTimeSlots.Any(slot =>
                !TryGetSlotStart(date, slot, out var start) || start <= now)))
            return (null, "A schedule can only be edited before its slot starts.");

        var booked = await _scheduleRepo.GetBookedSlotsMapAsync(id, ct);
        var requestedKeys = request.AvailableDates
            .SelectMany(date => request.AvailableTimeSlots.Select(slot => SlotKey(date, slot)))
            .ToHashSet();
        if (booked.Any(item => item.Value > 0 && !requestedKeys.Contains(item.Key)))
            return (null, "Booked slots cannot be removed while editing a schedule.");

        var updated = new BookingSchedule
        {
            Id = id,
            DestinationId = request.DestinationId,
            DestinationTitle = request.DestinationTitle,
            Location = request.Location,
            GuideName = request.GuideName,
            PricePerPerson = request.PricePerPerson,
            MaxCapacityPerSlot = request.MaxCapacityPerSlot,
            Rating = request.Rating,
            ReviewsCount = request.ReviewsCount,
            AvailableDates = request.AvailableDates.Select(date => new ScheduleAvailableDate
            {
                BookingScheduleId = id,
                Date = date.ToUniversalTime()
            }).ToList(),
            TimeSlots = request.AvailableTimeSlots.Select(slot => new ScheduleTimeSlot
            {
                BookingScheduleId = id,
                SlotLabel = slot
            }).ToList()
        };

        var saved = await _scheduleRepo.UpdateAsync(updated, ct);
        return saved == null
            ? (null, "Schedule could not be updated.")
            : (BookingMapper.ToResponse(saved, await _scheduleRepo.GetBookedSlotsMapAsync(id, ct)), null);
    }

    public async Task<(bool Success, string? Error)> DeleteScheduleAsync(Guid id, CancellationToken ct = default)
    {
        var schedule = await _scheduleRepo.GetByIdAsync(id, ct);
        if (schedule == null) return (false, "Schedule not found.");

        var booked = await _scheduleRepo.GetBookedSlotsMapAsync(id, ct);
        if (booked.Any(item => item.Value > 0))
            return (false, "A schedule with bookings cannot be deleted.");
        if (schedule.AvailableDates.Count == 0 || schedule.TimeSlots.Count == 0 ||
            schedule.AvailableDates.Any(date => schedule.TimeSlots.Any(slot =>
                !TryGetSlotStart(date.Date, slot.SlotLabel, out var start) || start <= DateTime.UtcNow)))
            return (false, "Only fully upcoming schedules can be deleted.");

        return (await _scheduleRepo.DeleteAsync(id, ct), null);
    }

    private static bool TryGetSlotStart(DateTime date, string slot, out DateTime start)
    {
        if (!DateTime.TryParseExact(
                slot.Trim(),
                new[] { "h:mm tt", "hh:mm tt" },
                CultureInfo.InvariantCulture,
                DateTimeStyles.AllowWhiteSpaces,
                out var time))
        {
            start = default;
            return false;
        }

        start = DateTime.SpecifyKind(date.Date.Add(time.TimeOfDay), DateTimeKind.Utc);
        return true;
    }

    private static string SlotKey(DateTime date, string slot) =>
        $"{date:yyyy-MM-dd}_{slot}";
}

// ─── BookingService ──────────────────────────────────────────────────────────

public class BookingService : IBookingService
{
    private readonly IBookingRepository _bookingRepo;
    private readonly IBookingScheduleRepository _scheduleRepo;

    public BookingService(IBookingRepository bookingRepo, IBookingScheduleRepository scheduleRepo)
    {
        _bookingRepo = bookingRepo;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<IEnumerable<BookingResponse>> GetTravelerBookingsAsync(Guid travelerId, int page, int pageSize, CancellationToken ct = default)
    {
        var bookings = await _bookingRepo.GetByTravelerIdAsync(travelerId, page, pageSize, ct);
        return bookings.Select(BookingMapper.ToResponse);
    }

    public async Task<IEnumerable<BookingResponse>> GetAllBookingsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var bookings = await _bookingRepo.GetAllAsync(page, Math.Clamp(pageSize, 1, 100), ct);
        return bookings.Select(BookingMapper.ToResponse);
    }

    public async Task<BookingResponse?> GetBookingByIdAsync(Guid id, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        return booking == null ? null : BookingMapper.ToResponse(booking);
    }

    public async Task<(BookingResponse? Booking, string? Error)> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default)
    {
        // Fetch schedule + validate capacity
        var schedule = await _scheduleRepo.GetByIdAsync(request.ScheduleId, ct);
        if (schedule == null)
            return (null, "Schedule not found.");

        var bookedMap = await _scheduleRepo.GetBookedSlotsMapAsync(request.ScheduleId, ct);
        var slotKey = $"{request.BookingDate:yyyy-MM-dd}_{request.TimeSlot}";
        var alreadyBooked = bookedMap.GetValueOrDefault(slotKey, 0);
        var remaining = schedule.MaxCapacityPerSlot - alreadyBooked;

        if (remaining < request.Guests)
        {
            var msg = remaining <= 0
                ? "Selected slot is completely full. Please choose another."
                : $"Only {remaining} spot(s) remaining, but {request.Guests} requested.";
            return (null, msg);
        }

        // Validate the time slot and date are offered
        var slotExists = schedule.TimeSlots.Any(t => t.SlotLabel == request.TimeSlot);
        var reqDateUtc = request.BookingDate.Kind == DateTimeKind.Utc
            ? request.BookingDate
            : request.BookingDate.ToUniversalTime();
        var dateExists = schedule.AvailableDates.Any(d =>
            d.Date.Date == request.BookingDate.Date ||
            d.Date.Date == reqDateUtc.Date);
        if (!slotExists || !dateExists)
            return (null, "The requested time slot or date is not available for this schedule.");

        if (!Enum.TryParse<BookingPaymentMethod>(request.PaymentMethod, true, out var paymentMethod))
            return (null, "A valid payment method is required.");

        if (paymentMethod != BookingPaymentMethod.SampleCard &&
            string.IsNullOrWhiteSpace(request.ReceiptImageData))
            return (null, "A receipt image is required for this payment method.");

        // Apply the automatic group discount for bookings with more than six guests.
        var basePrice = schedule.PricePerPerson * request.Guests;
        var serviceFee = Math.Round(basePrice * 0.05m, 2); // 5% service fee
        var discountAmount = request.Guests > 6
            ? Math.Round(basePrice * 0.20m, 2)
            : 0m;
        var total = basePrice + serviceFee - discountAmount;

        var bookingRef = $"BKG-{DateTime.UtcNow.Ticks.ToString()[^5..]}";

        var booking = new Booking
        {
            BookingReference = bookingRef,
            ScheduleId = request.ScheduleId,
            DestinationTitle = schedule.DestinationTitle,
            Location = schedule.Location,
            TravelerId = request.TravelerId,
            TravelerName = request.TravelerName,
            TravelerEmail = request.TravelerEmail,
            BookingDate = DateTime.SpecifyKind(request.BookingDate, DateTimeKind.Utc),
            TimeSlot = request.TimeSlot,
            Guests = request.Guests,
            BasePrice = basePrice,
            ServiceFee = serviceFee,
            DiscountAmount = discountAmount,
            TotalAmount = total,
            Status = BookingStatus.Pending,
            PaymentStatus = EscrowStatus.Pending,
            PaymentMethod = paymentMethod,
            ReceiptReference = request.ReceiptReference,
            ReceiptImageData = request.ReceiptImageData,
            Notes = request.Notes
        };

        var created = await _bookingRepo.CreateAsync(booking, ct);
        return (BookingMapper.ToResponse(created), null);
    }

    public async Task<BookingResponse?> UpdateBookingStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<BookingStatus>(status, true, out var parsedStatus))
            return null;

        var booking = await _bookingRepo.GetByIdAsync(id, ct);
        if (booking == null) return null;

        booking.Status = parsedStatus;
        var updated = await _bookingRepo.UpdateAsync(booking, ct);
        return updated == null ? null : BookingMapper.ToResponse(updated);
    }

    public async Task<bool> CancelBookingAsync(Guid id, CancellationToken ct = default)
    {
        return await _bookingRepo.DeleteAsync(id, ct);
    }
}

// ─── DiscountRequestService ──────────────────────────────────────────────────

public class DiscountRequestService : IDiscountRequestService
{
    private readonly IDiscountRequestRepository _discountRepo;
    private readonly IBookingRepository _bookingRepo;
    private readonly IPaymentEscrowRepository _escrowRepo;

    public DiscountRequestService(
        IDiscountRequestRepository discountRepo,
        IBookingRepository bookingRepo,
        IPaymentEscrowRepository escrowRepo)
    {
        _discountRepo = discountRepo;
        _bookingRepo = bookingRepo;
        _escrowRepo = escrowRepo;
    }

    public async Task<IEnumerable<DiscountRequestResponse>> GetAllDiscountRequestsAsync(CancellationToken ct = default)
    {
        var requests = await _discountRepo.GetAllAsync(ct);
        return requests.Select(BookingMapper.ToResponse);
    }

    public async Task<IEnumerable<DiscountRequestResponse>> GetTravelerDiscountRequestsAsync(
        Guid travelerId, CancellationToken ct = default)
    {
        var requests = await _discountRepo.GetByTravelerIdAsync(travelerId, ct);
        return requests.Select(BookingMapper.ToResponse);
    }

    public async Task<DiscountRequestResponse?> UpdateDiscountRequestStatusAsync(Guid id, string status, CancellationToken ct = default)
    {
        if (!Enum.TryParse<DiscountStatus>(status, true, out var parsedStatus)) return null;
        var request = await _discountRepo.GetByIdAsync(id, ct);
        if (request == null) return null;
        if (request.Status == parsedStatus) return BookingMapper.ToResponse(request);
        request.Status = parsedStatus;
        var updated = await _discountRepo.UpdateAsync(request, ct);
        if (updated != null && parsedStatus == DiscountStatus.Approved)
        {
            var booking = await _bookingRepo.GetByIdAsync(request.BookingId, ct);
            if (booking != null)
            {
                booking.DiscountAmount += request.CalculatedDiscountAmount;
                booking.TotalAmount = Math.Max(0, booking.TotalAmount - request.CalculatedDiscountAmount);
                booking.Notes = $"Discount approved ({request.RequestedDiscountPercent:0}%): {request.Reason}";
                await _bookingRepo.UpdateAsync(booking, ct);

                var escrow = await _escrowRepo.GetByBookingIdAsync(request.BookingId, ct);
                if (escrow != null && escrow.Status == EscrowStatus.HeldInEscrow)
                {
                    escrow.Amount = Math.Max(0, escrow.Amount - request.CalculatedDiscountAmount);
                    escrow.RefundedAmount += request.CalculatedDiscountAmount;
                    await _escrowRepo.UpdateAsync(escrow, ct);
                }
            }
        }
        return updated == null ? null : BookingMapper.ToResponse(updated);
    }

    public async Task<(DiscountRequestResponse? Result, string? Error)> CreateDiscountRequestAsync(
        CreateDiscountRequestRequest request, CancellationToken ct = default)
    {
        if (request.RequestedDiscountPercent <= 0 || request.RequestedDiscountPercent > 50)
            return (null, "Discount percent must be between 1 and 50.");

        var booking = await _bookingRepo.GetByIdAsync(request.BookingId, ct);
        if (booking == null)
            return (null, "Booking not found.");

        if (booking.Status == BookingStatus.Cancelled || booking.Status == BookingStatus.Completed)
            return (null, "Cannot request discount on a cancelled or completed booking.");

        var existing = await _discountRepo.GetByBookingIdAsync(request.BookingId, ct);
        if (existing != null)
            return (null, "A discount request already exists for this booking.");

        var discountReq = new DiscountRequest
        {
            BookingId = request.BookingId,
            ScheduleId = request.ScheduleId,
            TravelerId = request.TravelerId,
            OriginalPrice = request.OriginalPrice,
            RequestedDiscountPercent = request.RequestedDiscountPercent,
            Reason = request.Reason,
            Status = DiscountStatus.Pending
        };

        var created = await _discountRepo.CreateAsync(discountReq, ct);

        return (BookingMapper.ToResponse(created), null);
    }
}

// ─── PaymentEscrowService ─────────────────────────────────────────────────────

public class PaymentEscrowService : IPaymentEscrowService
{
    private readonly IPaymentEscrowRepository _escrowRepo;
    private readonly IBookingRepository _bookingRepo;

    public PaymentEscrowService(IPaymentEscrowRepository escrowRepo, IBookingRepository bookingRepo)
    {
        _escrowRepo = escrowRepo;
        _bookingRepo = bookingRepo;
    }

    public async Task<(PaymentEscrowResponse? Result, string? Error)> ProcessEscrowPaymentAsync(
        ProcessEscrowPaymentRequest request, CancellationToken ct = default)
    {
        var booking = await _bookingRepo.GetByIdAsync(request.BookingId, ct);
        if (booking == null)
            return (null, "Booking not found.");

        if (booking.Status == BookingStatus.Cancelled)
            return (null, "Cannot process payment for a cancelled booking.");

        if (booking.PaymentStatus == EscrowStatus.HeldInEscrow)
            return (null, "Payment is already held in escrow for this booking.");

        // Generate transaction reference (sandbox simulation)
        var txnRef = $"txn_sandbox_{request.PaymentMethodId[..Math.Min(8, request.PaymentMethodId.Length)]}_{DateTime.UtcNow.Ticks}";
        var releaseDate = DateTime.UtcNow.AddDays(3);

        var escrow = new PaymentEscrow
        {
            BookingId = request.BookingId,
            Amount = request.TotalAmount,
            Status = EscrowStatus.HeldInEscrow,
            TransactionRef = txnRef,
            EscrowReleaseDate = releaseDate
        };

        var created = await _escrowRepo.CreateAsync(escrow, ct);

        // Update booking to confirmed + record transaction
        booking.Status = BookingStatus.Confirmed;
        booking.PaymentStatus = EscrowStatus.HeldInEscrow;
        booking.TransactionRef = txnRef;
        booking.EscrowReleaseDate = releaseDate;
        await _bookingRepo.UpdateAsync(booking, ct);

        return (BookingMapper.ToResponse(created), null);
    }
}
