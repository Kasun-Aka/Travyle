namespace Travyle.Api.Models;

public class PaymentEscrow
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid BookingId { get; set; }

    public decimal Amount { get; set; }
    public decimal RefundedAmount { get; set; }
    public EscrowStatus Status { get; set; } = EscrowStatus.HeldInEscrow;
    public string TransactionRef { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime EscrowReleaseDate { get; set; }
    public DateTime? ReleasedAt { get; set; }

    // Navigation
    public Booking Booking { get; set; } = null!;
}
