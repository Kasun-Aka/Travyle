using Travyle.Api.Models;

namespace Travyle.Api.Repositories;

public interface IBookingScheduleRepository
{
    Task<IEnumerable<BookingSchedule>> GetAllAsync(string? search, DateTime? filterDate, CancellationToken ct = default);
    Task<BookingSchedule?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<BookingSchedule> CreateAsync(BookingSchedule schedule, CancellationToken ct = default);
    Task<BookingSchedule?> UpdateAsync(BookingSchedule schedule, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
    Task<Dictionary<string, int>> GetBookedSlotsMapAsync(Guid scheduleId, CancellationToken ct = default);
}

public interface IBookingRepository
{
    Task<IEnumerable<Booking>> GetAllAsync(int page, int pageSize, CancellationToken ct = default);
    Task<IEnumerable<Booking>> GetByTravelerIdAsync(Guid travelerId, int page, int pageSize, CancellationToken ct = default);
    Task<Booking?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<Booking> CreateAsync(Booking booking, CancellationToken ct = default);
    Task<Booking?> UpdateAsync(Booking booking, CancellationToken ct = default);
    Task<bool> DeleteAsync(Guid id, CancellationToken ct = default);
}

public interface IDiscountRequestRepository
{
    Task<IEnumerable<DiscountRequest>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<DiscountRequest>> GetByTravelerIdAsync(Guid travelerId, CancellationToken ct = default);
    Task<DiscountRequest?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<DiscountRequest?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<DiscountRequest> CreateAsync(DiscountRequest request, CancellationToken ct = default);
    Task<DiscountRequest?> UpdateAsync(DiscountRequest request, CancellationToken ct = default);
}

public interface IPaymentEscrowRepository
{
    Task<PaymentEscrow?> GetByBookingIdAsync(Guid bookingId, CancellationToken ct = default);
    Task<PaymentEscrow> CreateAsync(PaymentEscrow escrow, CancellationToken ct = default);
    Task<PaymentEscrow?> UpdateAsync(PaymentEscrow escrow, CancellationToken ct = default);
}
