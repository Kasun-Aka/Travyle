using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services.Agent;

namespace Travyle.Api.Services;

public class SupportService : ISupportService
{
    private readonly TravyleDbContext _dbContext;
    private readonly ISupportAiAgentService _aiAgentService;
    private readonly INotificationService _notificationService;
    private readonly ILogger<SupportService> _logger;
    private readonly ISupportAttachmentStorage _attachmentStorage;

    public SupportService(
        TravyleDbContext dbContext,
        ISupportAiAgentService aiAgentService,
        INotificationService notificationService,
        ILogger<SupportService> logger,
        ISupportAttachmentStorage? attachmentStorage = null)
    {
        _dbContext = dbContext;
        _aiAgentService = aiAgentService;
        _notificationService = notificationService;
        _logger = logger;
        _attachmentStorage = attachmentStorage ?? new LocalSupportAttachmentStorage();
    }

    private async Task<User> EnsureUserAsync(Guid? userId, CancellationToken cancellationToken)
    {
        if (userId.HasValue)
        {
            var existing = await _dbContext.Users.FirstOrDefaultAsync(u => u.Id == userId.Value, cancellationToken);
            if (existing != null) return existing;
        }

        // Return first available user or create a default traveler
        var defaultUser = await _dbContext.Users.FirstOrDefaultAsync(cancellationToken);
        if (defaultUser == null)
        {
            defaultUser = new User
            {
                Id = userId ?? Guid.NewGuid(),
                Email = "traveler@travyle.com",
                FullName = "Alex Traveler",
                Role = "Traveler",
                CreatedAt = DateTime.UtcNow
            };
            await _dbContext.Users.AddAsync(defaultUser, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        return defaultUser;
    }

    public async Task<TicketResponseDto> CreateTicketAsync(CreateTicketDto dto, CancellationToken cancellationToken = default)
    {
        var user = await EnsureUserAsync(dto.UserId, cancellationToken);

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = dto.Title,
            Description = dto.Description,
            Category = string.IsNullOrWhiteSpace(dto.Category) ? "General" : dto.Category,
            Priority = dto.Priority,
            Status = TicketStatus.Pending_AI_Triage,
            BookingId = dto.BookingId,
            TourId = dto.TourId,
            AttachmentUrl = dto.AttachmentUrl,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.SupportTickets.AddAsync(ticket, cancellationToken);

        // Initial submission audit log
        var initialAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket.Id,
            Action = "TICKET_CREATED",
            ActorRole = "Traveler",
            ActorId = user.Id.ToString(),
            Details = $"Ticket created with priority '{ticket.Priority}' in category '{ticket.Category}'. Dispatched to AI triage queue.",
            MetadataJson = JsonSerializer.Serialize(new { ticket.Title, ticket.Category, ticket.Priority }),
            Timestamp = DateTime.UtcNow
        };

        await _dbContext.AuditLogs.AddAsync(initialAudit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        // Run automated AI Agent triage pipeline
        try
        {
            await _aiAgentService.TriageTicketAsync(ticket, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during automated AI triage for ticket {TicketId}", ticket.Id);
        }

        return await GetTicketByIdAsync(ticket.Id, cancellationToken)
            ?? throw new InvalidOperationException("Failed to retrieve created ticket");
    }

    public async Task<PaginatedListDto<TicketResponseDto>> GetTicketsAsync(TicketListQueryDto query, CancellationToken cancellationToken = default)
    {
        var queryable = _dbContext.SupportTickets
            .Include(t => t.User)
            .Include(t => t.AuditLogs)
            .Include(t => t.Vouchers)
            .Where(t => !t.IsDeleted)
            .AsQueryable();

        if (query.Priority.HasValue)
        {
            queryable = queryable.Where(t => t.Priority == query.Priority.Value);
        }

        if (query.Status.HasValue)
        {
            queryable = queryable.Where(t => t.Status == query.Status.Value);
        }

        if (query.UserId.HasValue)
        {
            queryable = queryable.Where(t => t.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.ToLower();
            queryable = queryable.Where(t =>
                t.Title.ToLower().Contains(search) ||
                t.Description.ToLower().Contains(search) ||
                t.Category.ToLower().Contains(search) ||
                (t.User != null && (t.User.FullName.ToLower().Contains(search) || t.User.Email.ToLower().Contains(search))));
        }

        var totalCount = await queryable.CountAsync(cancellationToken);

        var items = await queryable
            .OrderByDescending(t => t.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(t => new TicketResponseDto
            {
                Id = t.Id,
                UserId = t.UserId,
                UserName = t.User != null ? t.User.FullName : "Traveler",
                UserEmail = t.User != null ? t.User.Email : "traveler@travyle.com",
                BookingId = t.BookingId,
                TourId = t.TourId,
                Title = t.Title,
                Description = t.Description,
                Category = t.Category,
                Priority = t.Priority.ToString(),
                Status = t.Status.ToString(),
                AttachmentUrl = t.AttachmentUrl,
                SentimentScore = t.SentimentScore,
                SeverityTier = t.SeverityTier,
                AiReasoning = t.AiReasoning,
                ResolutionSummary = t.ResolutionSummary,
                CreatedAt = t.CreatedAt,
                UpdatedAt = t.UpdatedAt,
                AuditLogs = t.AuditLogs.OrderBy(a => a.Timestamp).Select(a => new AuditLogResponseDto
                {
                    Id = a.Id,
                    SupportTicketId = a.SupportTicketId,
                    Action = a.Action,
                    ActorRole = a.ActorRole,
                    ActorId = a.ActorId,
                    Details = a.Details,
                    MetadataJson = a.MetadataJson,
                    Timestamp = a.Timestamp
                }).ToList(),
                Vouchers = t.Vouchers.Select(v => new VoucherResponseDto
                {
                    Id = v.Id,
                    Code = v.Code,
                    UserId = v.UserId,
                    Amount = v.Amount,
                    Reason = v.Reason,
                    Status = v.Status.ToString(),
                    ApprovedByAdminId = v.ApprovedByAdminId,
                    IssuedAt = v.IssuedAt,
                    ExpiresAt = v.ExpiresAt,
                    CreatedAt = v.CreatedAt
                }).ToList()
            })
            .ToListAsync(cancellationToken);

        return new PaginatedListDto<TicketResponseDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = query.Page,
            PageSize = query.PageSize
        };
    }

    public async Task<TicketResponseDto?> GetTicketByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var t = await _dbContext.SupportTickets
            .Include(x => x.User)
            .Include(x => x.AuditLogs)
            .Include(x => x.Vouchers)
            .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

        if (t == null) return null;

        return new TicketResponseDto
        {
            Id = t.Id,
            UserId = t.UserId,
            UserName = t.User?.FullName ?? "Traveler",
            UserEmail = t.User?.Email ?? "traveler@travyle.com",
            BookingId = t.BookingId,
            TourId = t.TourId,
            Title = t.Title,
            Description = t.Description,
            Category = t.Category,
            Priority = t.Priority.ToString(),
            Status = t.Status.ToString(),
            AttachmentUrl = t.AttachmentUrl,
            SentimentScore = t.SentimentScore,
            SeverityTier = t.SeverityTier,
            AiReasoning = t.AiReasoning,
            ResolutionSummary = t.ResolutionSummary,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            AuditLogs = t.AuditLogs.OrderBy(a => a.Timestamp).Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                SupportTicketId = a.SupportTicketId,
                Action = a.Action,
                ActorRole = a.ActorRole,
                ActorId = a.ActorId,
                Details = a.Details,
                MetadataJson = a.MetadataJson,
                Timestamp = a.Timestamp
            }).ToList(),
            Vouchers = t.Vouchers.Select(v => new VoucherResponseDto
            {
                Id = v.Id,
                Code = v.Code,
                UserId = v.UserId,
                UserName = t.User?.FullName,
                SupportTicketId = v.SupportTicketId,
                Amount = v.Amount,
                Reason = v.Reason,
                Status = v.Status.ToString(),
                ApprovedByAdminId = v.ApprovedByAdminId,
                IssuedAt = v.IssuedAt,
                ExpiresAt = v.ExpiresAt,
                RedeemedAt = v.RedeemedAt,
                CreatedAt = v.CreatedAt
            }).ToList()
        };
    }

    public async Task<TicketResponseDto?> UpdateTicketStatusAsync(Guid id, UpdateTicketStatusDto dto, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket == null) return null;

        ticket.Status = dto.Status;
        if (!string.IsNullOrWhiteSpace(dto.ResolutionSummary))
        {
            ticket.ResolutionSummary = dto.ResolutionSummary;
        }
        if (dto.AdminId.HasValue)
        {
            ticket.AssignedToAdminId = dto.AdminId.Value;
        }
        ticket.UpdatedAt = DateTime.UtcNow;

        var statusAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket.Id,
            Action = "STATUS_UPDATED",
            ActorRole = "Support_Admin",
            ActorId = dto.AdminId?.ToString() ?? "Admin",
            Details = $"Ticket status changed to '{dto.Status}'. Resolution: '{dto.ResolutionSummary ?? "None provided"}'.",
            MetadataJson = JsonSerializer.Serialize(new { newStatus = dto.Status.ToString(), dto.ResolutionSummary }),
            Timestamp = DateTime.UtcNow
        };

        return await GetTicketByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<TicketResponseDto?> UpdateTicketAsync(Guid id, UpdateTicketDto dto, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        if (ticket == null) return null;

        // Restriction: Do not allow editing closed tickets
        if (ticket.Status == TicketStatus.Closed)
        {
            throw new InvalidOperationException("Cannot edit a ticket that is already closed.");
        }

        ticket.Title = dto.Title;
        ticket.Description = dto.Description;
        ticket.Category = dto.Category;
        ticket.Priority = dto.Priority;
        if (!string.IsNullOrWhiteSpace(dto.AttachmentUrl))
        {
            ticket.AttachmentUrl = dto.AttachmentUrl;
        }
        ticket.UpdatedAt = DateTime.UtcNow;

        var audit = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket.Id,
            Action = "TICKET_UPDATED",
            ActorRole = "Traveler",
            ActorId = ticket.UserId.ToString(),
            Details = $"Ticket details updated (Category: {dto.Category}, Priority: {dto.Priority}).",
            MetadataJson = JsonSerializer.Serialize(new { dto.Title, dto.Category, dto.Priority }),
            Timestamp = DateTime.UtcNow
        };

        await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return await GetTicketByIdAsync(ticket.Id, cancellationToken);
    }

    public async Task<bool> DeleteTicketAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == id && !t.IsDeleted, cancellationToken);
        if (ticket == null) return false;

        ticket.IsDeleted = true;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<AutoResolveResultDto?> AutoResolveClaimAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var ticket = await _dbContext.SupportTickets.FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
        if (ticket == null) return null;

        return await _aiAgentService.TriageTicketAsync(ticket, cancellationToken);
    }

    public Task<string> UploadAttachmentAsync(IFormFile file, HttpRequest request, CancellationToken cancellationToken = default)
    {
        // Validation (type, size, real image content) and storage are handled by ISupportAttachmentStorage.
        return _attachmentStorage.SaveAsync(file, request, cancellationToken);
    }

    public async Task<VoucherResponseDto> IssueVoucherAsync(CreateVoucherDto dto, CancellationToken cancellationToken = default)
    {
        var user = await EnsureUserAsync(dto.UserId, cancellationToken);
        var code = $"TRAV-{Random.Shared.Next(100, 999)}-{Guid.NewGuid().ToString()[..4].ToUpper()}";

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            Code = code,
            UserId = user.Id,
            SupportTicketId = dto.SupportTicketId,
            Amount = dto.Amount,
            Reason = dto.Reason,
            Status = VoucherStatus.Active,
            IssuedAt = DateTime.UtcNow,
            ExpiresAt = DateTime.UtcNow.AddDays(dto.ExpiryDays),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.Vouchers.AddAsync(voucher, cancellationToken);

        if (dto.SupportTicketId.HasValue)
        {
            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                SupportTicketId = dto.SupportTicketId.Value,
                Action = "DIRECT_VOUCHER_ISSUED",
                ActorRole = "Support_Admin",
                Details = $"Directly issued active ${voucher.Amount} Voucher [{voucher.Code}].",
                MetadataJson = JsonSerializer.Serialize(new { voucher.Code, voucher.Amount }),
                Timestamp = DateTime.UtcNow
            };
            await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Send customer notification
        await _notificationService.SendVoucherIssuedNotificationAsync(user.Email, user.FullName, voucher.Code, voucher.Amount, voucher.Reason);

        return new VoucherResponseDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            UserId = voucher.UserId,
            UserName = user.FullName,
            SupportTicketId = voucher.SupportTicketId,
            Amount = voucher.Amount,
            Reason = voucher.Reason,
            Status = voucher.Status.ToString(),
            IssuedAt = voucher.IssuedAt,
            ExpiresAt = voucher.ExpiresAt,
            CreatedAt = voucher.CreatedAt
        };
    }

    public async Task<VoucherResponseDto?> ApproveVoucherAsync(Guid voucherId, ApproveVoucherDto dto, CancellationToken cancellationToken = default)
    {
        var voucher = await _dbContext.Vouchers
            .Include(v => v.User)
            .Include(v => v.SupportTicket)
            .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);

        if (voucher == null) return null;

        if (dto.AdjustedAmount.HasValue && dto.AdjustedAmount > 0)
        {
            voucher.Amount = dto.AdjustedAmount.Value;
        }

        voucher.Status = VoucherStatus.Active;
        voucher.ApprovedByAdminId = dto.AdminId;
        voucher.IssuedAt = DateTime.UtcNow;
        voucher.UpdatedAt = DateTime.UtcNow;

        // Also resolve associated Support Ticket if linked
        if (voucher.SupportTicket != null)
        {
            voucher.SupportTicket.Status = TicketStatus.Resolved;
            voucher.SupportTicket.ResolutionSummary = $"Approved ${voucher.Amount:F2} Goodwill Voucher [{voucher.Code}]. {dto.Notes}";
            voucher.SupportTicket.UpdatedAt = DateTime.UtcNow;

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                SupportTicketId = voucher.SupportTicket.Id,
                Action = "VOUCHER_APPROVED_AND_ACTIVATED",
                ActorRole = "Support_Admin",
                ActorId = dto.AdminId?.ToString() ?? "Admin",
                Details = $"Admin approved ${voucher.Amount:F2} Goodwill Voucher [{voucher.Code}]. Notification triggered to customer.",
                MetadataJson = JsonSerializer.Serialize(new { voucher.Code, voucher.Amount, dto.Notes }),
                Timestamp = DateTime.UtcNow
            };
            await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        // Trigger SendGrid / Twilio Notification
        if (dto.SendNotification && voucher.User != null)
        {
            await _notificationService.SendVoucherIssuedNotificationAsync(
                voucher.User.Email,
                voucher.User.FullName,
                voucher.Code,
                voucher.Amount,
                voucher.Reason
            );
        }

        return new VoucherResponseDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            UserId = voucher.UserId,
            UserName = voucher.User?.FullName,
            SupportTicketId = voucher.SupportTicketId,
            Amount = voucher.Amount,
            Reason = voucher.Reason,
            Status = voucher.Status.ToString(),
            ApprovedByAdminId = voucher.ApprovedByAdminId,
            IssuedAt = voucher.IssuedAt,
            ExpiresAt = voucher.ExpiresAt,
            CreatedAt = voucher.CreatedAt
        };
    }

    public async Task<VoucherResponseDto?> RejectVoucherAsync(Guid voucherId, RejectVoucherDto dto, CancellationToken cancellationToken = default)
    {
        var voucher = await _dbContext.Vouchers
            .Include(v => v.User)
            .Include(v => v.SupportTicket)
            .FirstOrDefaultAsync(v => v.Id == voucherId, cancellationToken);

        if (voucher == null) return null;

        voucher.Status = VoucherStatus.Revoked;
        voucher.ApprovedByAdminId = dto.AdminId;
        voucher.UpdatedAt = DateTime.UtcNow;

        if (voucher.SupportTicket != null)
        {
            voucher.SupportTicket.Status = TicketStatus.Rejected;
            voucher.SupportTicket.ResolutionSummary = $"Declined Goodwill Voucher draft. Reason: {dto.Reason ?? "Not approved by admin"}";
            voucher.SupportTicket.UpdatedAt = DateTime.UtcNow;

            var audit = new AuditLog
            {
                Id = Guid.NewGuid(),
                SupportTicketId = voucher.SupportTicket.Id,
                Action = "VOUCHER_REJECTED",
                ActorRole = "Support_Admin",
                ActorId = dto.AdminId?.ToString() ?? "Admin",
                Details = $"Admin rejected Goodwill Voucher [{voucher.Code}]. Reason: {dto.Reason ?? "None provided"}.",
                MetadataJson = JsonSerializer.Serialize(new { voucher.Code, dto.Reason }),
                Timestamp = DateTime.UtcNow
            };
            await _dbContext.AuditLogs.AddAsync(audit, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new VoucherResponseDto
        {
            Id = voucher.Id,
            Code = voucher.Code,
            UserId = voucher.UserId,
            UserName = voucher.User?.FullName,
            SupportTicketId = voucher.SupportTicketId,
            Amount = voucher.Amount,
            Reason = voucher.Reason,
            Status = voucher.Status.ToString(),
            ApprovedByAdminId = voucher.ApprovedByAdminId,
            IssuedAt = voucher.IssuedAt,
            ExpiresAt = voucher.ExpiresAt,
            CreatedAt = voucher.CreatedAt
        };
    }

    public async Task<VoucherResponseDto?> GetVoucherByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var v = await _dbContext.Vouchers
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted, cancellationToken);

        if (v == null) return null;

        return new VoucherResponseDto
        {
            Id = v.Id,
            Code = v.Code,
            UserId = v.UserId,
            UserName = v.User?.FullName ?? "Traveler",
            SupportTicketId = v.SupportTicketId,
            Amount = v.Amount,
            Reason = v.Reason,
            Status = v.Status.ToString(),
            ApprovedByAdminId = v.ApprovedByAdminId,
            IssuedAt = v.IssuedAt,
            ExpiresAt = v.ExpiresAt,
            RedeemedAt = v.RedeemedAt,
            CreatedAt = v.CreatedAt
        };
    }

    public async Task<List<VoucherResponseDto>> GetAllVouchersAsync(string? status = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.Vouchers
            .Include(v => v.User)
            .Where(v => !v.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && status != "ALL")
        {
            if (Enum.TryParse<VoucherStatus>(status, true, out var parsedStatus))
            {
                query = query.Where(v => v.Status == parsedStatus);
            }
        }

        return await query
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VoucherResponseDto
            {
                Id = v.Id,
                Code = v.Code,
                UserId = v.UserId,
                UserName = v.User != null ? v.User.FullName : "Traveler",
                SupportTicketId = v.SupportTicketId,
                Amount = v.Amount,
                Reason = v.Reason,
                Status = v.Status.ToString(),
                ApprovedByAdminId = v.ApprovedByAdminId,
                IssuedAt = v.IssuedAt,
                ExpiresAt = v.ExpiresAt,
                RedeemedAt = v.RedeemedAt,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<List<VoucherResponseDto>> GetVouchersByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Vouchers
            .Where(v => v.UserId == userId && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .Select(v => new VoucherResponseDto
            {
                Id = v.Id,
                Code = v.Code,
                UserId = v.UserId,
                SupportTicketId = v.SupportTicketId,
                Amount = v.Amount,
                Reason = v.Reason,
                Status = v.Status.ToString(),
                ApprovedByAdminId = v.ApprovedByAdminId,
                IssuedAt = v.IssuedAt,
                ExpiresAt = v.ExpiresAt,
                RedeemedAt = v.RedeemedAt,
                CreatedAt = v.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> DeleteVoucherAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var voucher = await _dbContext.Vouchers.FirstOrDefaultAsync(v => v.Id == id && !v.IsDeleted, cancellationToken);
        if (voucher == null) return false;

        voucher.IsDeleted = true;
        voucher.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<ReviewResponseDto>> GetReviewsByTourIdAsync(Guid? tourId = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.CustomerReviews.Include(r => r.User).AsQueryable();
        if (tourId.HasValue && tourId.Value != Guid.Empty)
        {
            query = query.Where(r => r.TourId == tourId.Value);
        }

        return await query
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new ReviewResponseDto
            {
                Id = r.Id,
                UserId = r.UserId,
                UserName = r.User != null ? r.User.FullName : "Verified Traveler",
                TourId = r.TourId,
                Rating = r.Rating,
                Comment = r.Comment,
                IsVerified = r.IsVerified,
                CreatedAt = r.CreatedAt
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReviewResponseDto> CreateReviewAsync(CreateReviewDto dto, CancellationToken cancellationToken = default)
    {
        var user = await EnsureUserAsync(dto.UserId, cancellationToken);

        var review = new CustomerReview
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TourId = dto.TourId,
            Rating = dto.Rating,
            Comment = dto.Comment,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dbContext.CustomerReviews.AddAsync(review, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReviewResponseDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserName = user.FullName,
            TourId = review.TourId,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVerified = review.IsVerified,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<ReviewResponseDto?> ToggleReviewVerificationAsync(Guid id, bool isVerified, CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.CustomerReviews
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

        if (review == null) return null;

        review.IsVerified = isVerified;
        review.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        return new ReviewResponseDto
        {
            Id = review.Id,
            UserId = review.UserId,
            UserName = review.User?.FullName ?? "Verified Traveler",
            TourId = review.TourId,
            Rating = review.Rating,
            Comment = review.Comment,
            IsVerified = review.IsVerified,
            CreatedAt = review.CreatedAt
        };
    }

    public async Task<bool> DeleteReviewAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var review = await _dbContext.CustomerReviews.FirstOrDefaultAsync(r => r.Id == id, cancellationToken);
        if (review == null) return false;

        _dbContext.CustomerReviews.Remove(review);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
