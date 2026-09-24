using System.ComponentModel.DataAnnotations;
using Travyle.Api.Models;

namespace Travyle.Api.DTOs;

public class CreateTicketDto
{
    public Guid? UserId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [Required]
    public string Description { get; set; } = string.Empty;

    public string Category { get; set; } = "General";

    public TicketPriority Priority { get; set; } = TicketPriority.Medium;

    public Guid? BookingId { get; set; }
    public Guid? TourId { get; set; }

    public string? AttachmentUrl { get; set; }
}

public class UpdateTicketStatusDto
{
    [Required]
    public TicketStatus Status { get; set; }
    public string? ResolutionSummary { get; set; }
    public Guid? AdminId { get; set; }
}

public class TicketListQueryDto
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public TicketPriority? Priority { get; set; }
    public TicketStatus? Status { get; set; }
    public string? Search { get; set; }
    public Guid? UserId { get; set; }
}

public class PaginatedListDto<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
}

public class TicketResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }
    public Guid? BookingId { get; set; }
    public Guid? TourId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Priority { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? AttachmentUrl { get; set; }
    public double SentimentScore { get; set; }
    public string SeverityTier { get; set; } = string.Empty;
    public string? AiReasoning { get; set; }
    public string? ResolutionSummary { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<AuditLogResponseDto> AuditLogs { get; set; } = new();
    public List<VoucherResponseDto> Vouchers { get; set; } = new();
}

public class AutoResolveResultDto
{
    public Guid TicketId { get; set; }
    public double SentimentScore { get; set; }
    public string SeverityTier { get; set; } = string.Empty;
    public string SentimentSummary { get; set; } = string.Empty;
    public string AiReasoning { get; set; } = string.Empty;
    public string RecommendedAction { get; set; } = string.Empty;
    public bool VoucherDrafted { get; set; }
    public decimal? VoucherAmount { get; set; }
    public string? VoucherCode { get; set; }
    public TicketStatus NewStatus { get; set; }
    public List<AuditLogResponseDto> GeneratedAuditLogs { get; set; } = new();
}

public class CreateVoucherDto
{
    [Required]
    public Guid UserId { get; set; }
    public Guid? SupportTicketId { get; set; }
    public decimal Amount { get; set; } = 50.00m;
    public string Reason { get; set; } = "Goodwill compensation";
    public int ExpiryDays { get; set; } = 90;
}

public class ApproveVoucherDto
{
    public Guid? AdminId { get; set; }
    public decimal? AdjustedAmount { get; set; }
    public string? Notes { get; set; }
    public bool SendNotification { get; set; } = true;
}

public class VoucherResponseDto
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid? SupportTicketId { get; set; }
    public decimal Amount { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public Guid? ApprovedByAdminId { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RedeemedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateReviewDto
{
    public Guid? UserId { get; set; }

    [Required]
    public Guid TourId { get; set; }

    [Range(1, 5)]
    public int Rating { get; set; } = 5;

    [Required]
    [MaxLength(1000)]
    public string Comment { get; set; } = string.Empty;
}

public class ReviewResponseDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? UserName { get; set; }
    public Guid TourId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogResponseDto
{
    public Guid Id { get; set; }
    public Guid? SupportTicketId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string ActorRole { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string Details { get; set; } = string.Empty;
    public string? MetadataJson { get; set; }
    public DateTime Timestamp { get; set; }
}
