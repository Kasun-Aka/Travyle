using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Services;

public interface IBookingScheduleService
{
    Task<IEnumerable<BookingScheduleResponse>> GetSchedulesAsync(string? search, DateTime? filterDate, CancellationToken ct = default);
    Task<BookingScheduleResponse?> GetScheduleByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingScheduleResponse> CreateScheduleAsync(CreateBookingScheduleRequest request, CancellationToken ct = default);
    Task<(BookingScheduleResponse? Result, string? Error)> UpdateScheduleAsync(Guid id, UpdateBookingScheduleRequest request, CancellationToken ct = default);
    Task<(BookingScheduleResponse? Result, string? Error)> UpdateSlotAsync(Guid id, UpdateScheduleSlotRequest request, CancellationToken ct = default);
    Task<(BookingScheduleResponse? Result, string? Error)> DeleteSlotAsync(Guid id, DateTime date, string timeSlot, CancellationToken ct = default);
    Task<(bool Success, string? Error)> DeleteScheduleAsync(Guid id, CancellationToken ct = default);
}

public interface IBookingService
{
    Task<IEnumerable<BookingResponse>> GetAllBookingsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<BookingResponse>> GetTravelerBookingsAsync(Guid travelerId, int page, int pageSize, CancellationToken ct = default);
    Task<BookingResponse?> GetBookingByIdAsync(Guid id, CancellationToken ct = default);
    Task<(BookingResponse? Booking, string? Error)> CreateBookingAsync(CreateBookingRequest request, CancellationToken ct = default);
    Task<BookingResponse?> UpdateBookingStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<bool> CancelBookingAsync(Guid id, CancellationToken ct = default);
}

public interface IDiscountRequestService
{
    Task<IEnumerable<DiscountRequestResponse>> GetAllDiscountRequestsAsync(CancellationToken ct = default);
    Task<IEnumerable<DiscountRequestResponse>> GetTravelerDiscountRequestsAsync(Guid travelerId, CancellationToken ct = default);
    Task<DiscountRequestResponse?> UpdateDiscountRequestStatusAsync(Guid id, string status, CancellationToken ct = default);
    Task<(DiscountRequestResponse? Result, string? Error)> CreateDiscountRequestAsync(CreateDiscountRequestRequest request, CancellationToken ct = default);
}

public interface IPaymentEscrowService
{
    Task<(PaymentEscrowResponse? Result, string? Error)> ProcessEscrowPaymentAsync(ProcessEscrowPaymentRequest request, CancellationToken ct = default);
}
