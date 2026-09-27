using Travyle.Api.DTOs;

namespace Travyle.Api.Services.Agent;

public record BookingCapacityResult(
    Guid ScheduleId,
    DateTime Date,
    string TimeSlot,
    int RequestedGuests,
    int MaxCapacity,
    int BookedCount,
    int RemainingCapacity,
    bool IsAvailable,
    string? Message
);

public record BookingSummaryResult(
    Guid ScheduleId,
    string DestinationTitle,
    string Location,
    decimal PricePerPerson,
    int Guests,
    decimal BasePrice,
    decimal ServiceFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    bool GroupDiscountApplied
);

public interface IBookingAgentTools
{
    Task<IEnumerable<BookingScheduleResponse>> GetAvailableBookingSchedulesAsync(
        string? destinationQuery,
        DateTime? filterDate,
        CancellationToken ct = default);

    Task<BookingScheduleResponse?> GetBookingScheduleDetailsAsync(
        Guid scheduleId,
        CancellationToken ct = default);

    Task<BookingCapacityResult> CheckBookingCapacityAsync(
        Guid scheduleId,
        DateTime date,
        string timeSlot,
        int requestedGuests,
        CancellationToken ct = default);

    Task<IEnumerable<BookingResponse>> GetTravelerBookingsAsync(
        Guid travelerId,
        CancellationToken ct = default);

    Task<BookingSummaryResult?> CalculateBookingSummaryAsync(
        Guid scheduleId,
        int guests,
        CancellationToken ct = default);

    Task<(BookingResponse? Booking, string? Error)> CreateBookingAsync(
        CreateBookingRequest request,
        CancellationToken ct = default);

    Task<BookingResponse?> GetBookingStatusAsync(
        Guid bookingId,
        CancellationToken ct = default);
}
