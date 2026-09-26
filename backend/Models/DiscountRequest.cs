namespace Travyle.Api.Models;

public enum DiscountStatus
{
    Pending,
    Approved,
    Rejected
}

public class DiscountRequest
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }
    public Guid ScheduleId { get; set; }
    public Guid TravelerId { get; set; }

    public decimal OriginalPrice { get; set; }
    public decimal RequestedDiscountPercent { get; set; }
    public string Reason { get; set; } = string.Empty;

    public DiscountStatus Status { get; set; } = DiscountStatus.Pending;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Computed
    public decimal CalculatedDiscountAmount => OriginalPrice * RequestedDiscountPercent / 100m;

    // Navigation
    public Booking Booking { get; set; } = null!;
}
