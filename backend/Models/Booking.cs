namespace Travyle.Api.Models;

public enum BookingStatus
{
    Pending,
    Confirmed,
    Completed,
    Cancelled
}

public enum EscrowStatus
{
    Pending,
    HeldInEscrow,
    Released,
    Refunded
}

public enum BookingPaymentMethod
{
    SampleCard,
    BankTransferReceipt,
    AtmCashReceipt
}

public class Booking
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string BookingReference { get; set; } = string.Empty; // e.g. BKG-10101

    public Guid ScheduleId { get; set; }
    public string DestinationTitle { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;

    public Guid TravelerId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public string TravelerEmail { get; set; } = string.Empty;

    public DateTime BookingDate { get; set; }
    public string TimeSlot { get; set; } = string.Empty;
    public int Guests { get; set; }

    public decimal BasePrice { get; set; }
    public decimal ServiceFee { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }

    public BookingStatus Status { get; set; } = BookingStatus.Pending;
    public EscrowStatus PaymentStatus { get; set; } = EscrowStatus.Pending;
    public BookingPaymentMethod PaymentMethod { get; set; } = BookingPaymentMethod.SampleCard;

    public string? Notes { get; set; }
    public string? TransactionRef { get; set; }
    public string? ReceiptReference { get; set; }
    public string? ReceiptImageData { get; set; }
    public DateTime? EscrowReleaseDate { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation
    public BookingSchedule Schedule { get; set; } = null!;
    public PaymentEscrow? PaymentEscrow { get; set; }
    public DiscountRequest? DiscountRequest { get; set; }
}
