using Travyle.Api.DTOs;
using Travyle.Api.Repositories;

namespace Travyle.Api.Services.Agent;

public class BookingAgentTools : IBookingAgentTools
{
    private readonly IBookingScheduleService _scheduleService;
    private readonly IBookingService _bookingService;
    private readonly IBookingScheduleRepository _scheduleRepo;

    public BookingAgentTools(
        IBookingScheduleService scheduleService,
        IBookingService bookingService,
        IBookingScheduleRepository scheduleRepo)
    {
        _scheduleService = scheduleService;
        _bookingService = bookingService;
        _scheduleRepo = scheduleRepo;
    }

    public async Task<IEnumerable<BookingScheduleResponse>> GetAvailableBookingSchedulesAsync(
        string? destinationQuery,
        DateTime? filterDate,
        CancellationToken ct = default)
    {
        return await _scheduleService.GetSchedulesAsync(destinationQuery, filterDate, ct);
    }

    public async Task<BookingScheduleResponse?> GetBookingScheduleDetailsAsync(
        Guid scheduleId,
        CancellationToken ct = default)
    {
        if (scheduleId == Guid.Empty) return null;
        return await _scheduleService.GetScheduleByIdAsync(scheduleId, ct);
    }

    public async Task<BookingCapacityResult> CheckBookingCapacityAsync(
        Guid scheduleId,
        DateTime date,
        string timeSlot,
        int requestedGuests,
        CancellationToken ct = default)
    {
        if (scheduleId == Guid.Empty || requestedGuests <= 0 || string.IsNullOrWhiteSpace(timeSlot))
        {
            return new BookingCapacityResult(
                scheduleId, date, timeSlot ?? "", requestedGuests, 0, 0, 0, false, "Invalid capacity check parameters.");
        }

        var schedule = await _scheduleRepo.GetByIdAsync(scheduleId, ct);
        if (schedule == null)
        {
            return new BookingCapacityResult(
                scheduleId, date, timeSlot, requestedGuests, 0, 0, 0, false, "Schedule does not exist.");
        }

        var bookedMap = await _scheduleRepo.GetBookedSlotsMapAsync(scheduleId, ct);
        var slotKey = $"{date:yyyy-MM-dd}_{timeSlot}";
        var bookedCount = bookedMap.GetValueOrDefault(slotKey, 0);
        var remaining = schedule.MaxCapacityPerSlot - bookedCount;

        if (remaining < requestedGuests)
        {
            var msg = remaining <= 0
                ? $"Slot '{timeSlot}' on {date:yyyy-MM-dd} is completely full (0/{schedule.MaxCapacityPerSlot} available)."
                : $"Insufficient capacity. Only {remaining} spot(s) remaining for slot '{timeSlot}' on {date:yyyy-MM-dd}, but {requestedGuests} requested.";

            return new BookingCapacityResult(
                scheduleId, date, timeSlot, requestedGuests, schedule.MaxCapacityPerSlot, bookedCount, remaining, false, msg);
        }

        return new BookingCapacityResult(
            scheduleId, date, timeSlot, requestedGuests, schedule.MaxCapacityPerSlot, bookedCount, remaining, true, "Capacity available.");
    }

    public async Task<IEnumerable<BookingResponse>> GetTravelerBookingsAsync(
        Guid travelerId,
        CancellationToken ct = default)
    {
        if (travelerId == Guid.Empty) return Enumerable.Empty<BookingResponse>();
        return await _bookingService.GetTravelerBookingsAsync(travelerId, 1, 100, ct);
    }

    public async Task<BookingSummaryResult?> CalculateBookingSummaryAsync(
        Guid scheduleId,
        int guests,
        CancellationToken ct = default)
    {
        if (scheduleId == Guid.Empty || guests <= 0) return null;

        var schedule = await _scheduleRepo.GetByIdAsync(scheduleId, ct);
        if (schedule == null) return null;

        var basePrice = schedule.PricePerPerson * guests;
        var serviceFee = Math.Round(basePrice * 0.05m, 2);
        var groupDiscountApplied = guests > 6;
        var discountAmount = groupDiscountApplied ? Math.Round(basePrice * 0.20m, 2) : 0m;
        var totalAmount = basePrice + serviceFee - discountAmount;

        return new BookingSummaryResult(
            schedule.Id,
            schedule.DestinationTitle,
            schedule.Location,
            schedule.PricePerPerson,
            guests,
            basePrice,
            serviceFee,
            discountAmount,
            totalAmount,
            groupDiscountApplied
        );
    }

    public async Task<(BookingResponse? Booking, string? Error)> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default)
    {
        return await _bookingService.CreateBookingAsync(request, ct);
    }

    public async Task<BookingResponse?> GetBookingStatusAsync(
        Guid bookingId,
        CancellationToken ct = default)
    {
        return await _bookingService.GetBookingByIdAsync(bookingId, ct);
    }
}
