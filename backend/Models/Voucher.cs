using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace Travyle.Api.Models;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum VoucherStatus
{
    Draft,
    Active,
    Redeemed,
    Expired,
    Revoked
}

public class Voucher
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    [MaxLength(50)]
    public string Code { get; set; } = string.Empty;

    [Required]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? SupportTicketId { get; set; }
    public SupportTicket? SupportTicket { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; } = 50.00m;

    [MaxLength(200)]
    public string Reason { get; set; } = "Goodwill compensation";

    public VoucherStatus Status { get; set; } = VoucherStatus.Draft;

    public Guid? ApprovedByAdminId { get; set; }

    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RedeemedAt { get; set; }

    public bool IsDeleted { get; set; } = false;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
