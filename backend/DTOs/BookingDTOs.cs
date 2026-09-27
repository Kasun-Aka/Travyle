namespace Travyle.Api.DTOs;

// ─── BookingSchedule DTOs ───────────────────────────────────────────────────

public record BookingScheduleResponse(
    Guid Id,
    Guid DestinationId,
    string DestinationTitle,
    string Location,
    string GuideName,
    decimal PricePerPerson,
    int MaxCapacityPerSlot,
    double Rating,
    int ReviewsCount,
    List<DateTime> AvailableDates,
    List<string> AvailableTimeSlots,
    Dictionary<string, int> BookedSlotsMap,
    List<ScheduleSlotResponse> Slots
);

public record ScheduleSlotResponse(DateTime Date, string TimeSlot, int Booked);

public record UpdateScheduleSlotRequest(
    DateTime Date,
    string TimeSlot,
    DateTime NewDate,
    string NewTimeSlot
);

public record CreateBookingScheduleRequest(
    Guid DestinationId,
    string DestinationTitle,
    string Location,
    string GuideName,
    decimal PricePerPerson,
    int MaxCapacityPerSlot,
    double Rating,
    int ReviewsCount,
    List<DateTime> AvailableDates,
    List<string> AvailableTimeSlots
);

public record UpdateBookingScheduleRequest(
    Guid DestinationId,
    string DestinationTitle,
    string Location,
    string GuideName,
    decimal PricePerPerson,
    int MaxCapacityPerSlot,
    double Rating,
    int ReviewsCount,
    List<DateTime> AvailableDates,
    List<string> AvailableTimeSlots
);

// ─── Booking DTOs ────────────────────────────────────────────────────────────

public record BookingResponse(
    Guid Id,
    string BookingReference,
    Guid ScheduleId,
    string DestinationTitle,
    string Location,
    Guid TravelerId,
    string TravelerName,
    string TravelerEmail,
    DateTime BookingDate,
    string TimeSlot,
    int Guests,
    decimal BasePrice,
    decimal ServiceFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    string Status,
    string PaymentStatus,
    string PaymentMethod,
    string? Notes,
    string? TransactionRef,
    string? ReceiptReference,
    string? ReceiptImageData,
    DateTime? EscrowReleaseDate,
    DateTime CreatedAt
);

public record CreateBookingRequest(
    Guid ScheduleId,
    Guid TravelerId,
    string TravelerName,
    string TravelerEmail,
    DateTime BookingDate,
    string TimeSlot,
    int Guests,
    string? Notes,
    string PaymentMethod,
    string? ReceiptReference,
    string? ReceiptImageData
);

public record UpdateBookingStatusRequest(
    string Status
);

// ─── DiscountRequest DTOs ────────────────────────────────────────────────────

public record DiscountRequestResponse(
    Guid Id,
    Guid BookingId,
    Guid ScheduleId,
    Guid TravelerId,
    decimal OriginalPrice,
    decimal RequestedDiscountPercent,
    decimal CalculatedDiscountAmount,
    string Reason,
    string Status,
    DateTime CreatedAt
);

public record CreateDiscountRequestRequest(
    Guid BookingId,
    Guid ScheduleId,
    Guid TravelerId,
    decimal OriginalPrice,
    decimal RequestedDiscountPercent,
    string Reason
);

// ─── Escrow DTOs ─────────────────────────────────────────────────────────────

public record PaymentEscrowResponse(
    Guid Id,
    Guid BookingId,
    decimal Amount,
    string Status,
    string TransactionRef,
    DateTime CreatedAt,
    DateTime EscrowReleaseDate,
    DateTime? ReleasedAt
);

public record ProcessEscrowPaymentRequest(
    Guid BookingId,
    decimal TotalAmount,
    string PaymentMethodId
);
