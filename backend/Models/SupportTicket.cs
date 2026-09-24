using System.ComponentModel.DataAnnotations;

namespace Travyle.Api.Models;

public enum TicketPriority
{
    Low,
    Medium,
    High,
    Critical
}

public enum TicketStatus
{
    Pending_AI_Triage,
    Pending_Admin_Voucher_Approval,
    In_Review,
    Resolved,
    Closed,
    Rejected
}

public class SupportTicket
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    [Required]
    public Guid UserId { get; set; }
    public User? User { get; set; }

    public Guid? BookingId { get; set; }
    public Guid? TourId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    [MaxLength(50)]
    public string Category { get; set; } = "General"; // TourDelay, TourQuality, Safety, Billing, GuideConduct, General

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public TicketStatus Status { get; set; } = TicketStatus.Pending_AI_Triage;

    public string? AttachmentUrl { get; set; }

    // AI Triage & Analysis attributes
    public double SentimentScore { get; set; } = 0.0; // -1.0 (very negative) to +1.0 (very positive)
    public string SeverityTier { get; set; } = "Tier_1_Low"; // Tier_1_Low, Tier_2_High, Tier_3_Critical
    public string? AiReasoning { get; set; }
    public string? ResolutionSummary { get; set; }

    public Guid? AssignedToAdminId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Voucher> Vouchers { get; set; } = new List<Voucher>();
}
