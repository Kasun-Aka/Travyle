using System.ComponentModel.DataAnnotations;

namespace Travyle.Api.Models;

public class AuditLog
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid? SupportTicketId { get; set; }
    public SupportTicket? SupportTicket { get; set; }

    [Required]
    [MaxLength(100)]
    public string Action { get; set; } = string.Empty; // e.g. TICKET_CREATED, AI_SENTIMENT_ANALYZED, VOUCHER_DRAFTED, VOUCHER_APPROVED

    [Required]
    [MaxLength(50)]
    public string ActorRole { get; set; } = "System"; // Traveler, Support_AI_Agent, Support_Admin, System

    public string? ActorId { get; set; }

    [Required]
    public string Details { get; set; } = string.Empty;

    public string? MetadataJson { get; set; }

    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
