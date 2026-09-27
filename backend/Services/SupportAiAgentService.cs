using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Services;

public class SupportAiAgentService : ISupportAiAgentService
{
    private readonly TravyleDbContext _dbContext;
    private readonly ILogger<SupportAiAgentService> _logger;

    public SupportAiAgentService(TravyleDbContext dbContext, ILogger<SupportAiAgentService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<AutoResolveResultDto> TriageTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("[Support & Quality Agent] Commencing AI Triage for Ticket {TicketId}: {Title}", ticket.Id, ticket.Title);

        var auditEntries = new List<AuditLog>();

        // Step 1: Sentiment Analysis & Intensity Extraction
        var fullText = $"{ticket.Title} {ticket.Description}";
        var sentimentScore = await CalculateSentimentScoreAsync(fullText);

        var sentimentAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket.Id,
            Action = "AI_SENTIMENT_ANALYZED",
            ActorRole = "Support_AI_Agent",
            ActorId = "agent-sentiment-v1",
            Details = $"Evaluated customer ticket text. Computed sentiment score: {sentimentScore:F2} (Range: -1.0 to +1.0).",
            MetadataJson = JsonSerializer.Serialize(new { sentimentScore, analyzedAt = DateTime.UtcNow }),
            Timestamp = DateTime.UtcNow
        };
        auditEntries.Add(sentimentAudit);

        // Step 2: Severity Classification & Risk Tiering
        var severityTier = await DetermineSeverityTierAsync(fullText, sentimentScore, ticket.Priority);
        var severityAudit = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket.Id,
            Action = "AI_SEVERITY_CLASSIFIED",
            ActorRole = "Support_AI_Agent",
            ActorId = "agent-classifier-v1",
            Details = $"Classified ticket into severity tier: {severityTier}.",
            MetadataJson = JsonSerializer.Serialize(new { severityTier, initialPriority = ticket.Priority.ToString() }),
            Timestamp = DateTime.UtcNow.AddMilliseconds(100)
        };
        auditEntries.Add(severityAudit);

        // Step 3: Policy Cross-Check & Tool Evaluation (Customer History & Goodwill Rules)
        var (isEligible, voucherAmount, reason) = await EvaluateGoodwillEligibilityAsync(ticket, severityTier);

        bool voucherCreated = false;
        string? voucherCode = null;

        if (isEligible)
        {
            voucherCode = $"TRAV-GW-{Random.Shared.Next(1000, 9999)}-{DateTime.UtcNow:MMdd}";
            
            var draftVoucher = new Voucher
            {
                Id = Guid.NewGuid(),
                Code = voucherCode,
                UserId = ticket.UserId,
                SupportTicketId = ticket.Id,
                Amount = voucherAmount,
                Reason = reason,
                Status = VoucherStatus.Draft, // Paused for human administrator sign-off
                ExpiresAt = DateTime.UtcNow.AddDays(90),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _dbContext.Vouchers.AddAsync(draftVoucher, cancellationToken);
            voucherCreated = true;

            var voucherAudit = new AuditLog
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                Action = "AI_VOUCHER_DRAFTED",
                ActorRole = "Support_AI_Agent",
                ActorId = "agent-policy-v1",
                Details = $"Drafted ${voucherAmount:F2} Goodwill Voucher [{voucherCode}]. Pausing execution for human administrator approval.",
                MetadataJson = JsonSerializer.Serialize(new { voucherCode, amount = voucherAmount, reason, requiredApproval = "Support_Admin" }),
                Timestamp = DateTime.UtcNow.AddMilliseconds(200)
            };
            auditEntries.Add(voucherAudit);
        }
        else
        {
            var noVoucherAudit = new AuditLog
            {
                Id = Guid.NewGuid(),
                SupportTicketId = ticket.Id,
                Action = "AI_POLICY_CHECK_PASSED",
                ActorRole = "Support_AI_Agent",
                ActorId = "agent-policy-v1",
                Details = "Ticket reviewed against goodwill matrix: standard resolution route assigned (no financial compensation required).",
                MetadataJson = JsonSerializer.Serialize(new { outcome = "Standard_Support_Flow" }),
                Timestamp = DateTime.UtcNow.AddMilliseconds(200)
            };
            auditEntries.Add(noVoucherAudit);
        }

        // Update Ticket State
        ticket.SentimentScore = sentimentScore;
        ticket.SeverityTier = severityTier;
        ticket.AiReasoning = $"Sentiment: {sentimentScore:F2} | Tier: {severityTier} | Recommendation: {(voucherCreated ? $"Goodwill Voucher of ${voucherAmount:F2}" : "Standard Support Resolution")}";
        ticket.Status = voucherCreated ? TicketStatus.Pending_Admin_Voucher_Approval : TicketStatus.In_Review;
        ticket.UpdatedAt = DateTime.UtcNow;

        await _dbContext.AuditLogs.AddRangeAsync(auditEntries, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("[Support & Quality Agent] Completed Triage. New Status: {Status}", ticket.Status);

        return new AutoResolveResultDto
        {
            TicketId = ticket.Id,
            SentimentScore = sentimentScore,
            SeverityTier = severityTier,
            SentimentSummary = sentimentScore < -0.3 ? "Customer expressing frustration / dissatisfaction" : (sentimentScore > 0.3 ? "Customer inquiry / positive sentiment" : "Neutral / informational inquiry"),
            AiReasoning = ticket.AiReasoning,
            RecommendedAction = voucherCreated ? $"Issue ${voucherAmount:F2} Goodwill Discount Voucher" : "Standard customer service agent follow-up",
            VoucherDrafted = voucherCreated,
            VoucherAmount = voucherCreated ? voucherAmount : null,
            VoucherCode = voucherCode,
            NewStatus = ticket.Status,
            GeneratedAuditLogs = auditEntries.Select(a => new AuditLogResponseDto
            {
                Id = a.Id,
                SupportTicketId = a.SupportTicketId,
                Action = a.Action,
                ActorRole = a.ActorRole,
                ActorId = a.ActorId,
                Details = a.Details,
                MetadataJson = a.MetadataJson,
                Timestamp = a.Timestamp
            }).ToList()
        };
    }

    public Task<double> CalculateSentimentScoreAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return Task.FromResult(0.0);

        var lower = text.ToLowerInvariant();
        
        var negativeKeywords = new Dictionary<string, double>
        {
            { "horrible", -0.9 }, { "terrible", -0.9 }, { "worst", -0.95 }, { "disaster", -0.95 },
            { "delay", -0.6 }, { "delayed", -0.6 }, { "cancelled", -0.8 }, { "cancellation", -0.8 },
            { "refund", -0.5 }, { "complaint", -0.5 }, { "broken", -0.7 }, { "stolen", -0.9 },
            { "rude", -0.8 }, { "stranded", -0.9 }, { "unacceptable", -0.85 }, { "danger", -0.9 },
            { "unsafe", -0.95 }, { "poor", -0.5 }, { "bad", -0.5 }, { "awful", -0.85 },
            { "angry", -0.75 }, { "disappointed", -0.65 }, { "missed", -0.6 }, { "lost", -0.7 }
        };

        var positiveKeywords = new Dictionary<string, double>
        {
            { "great", 0.7 }, { "excellent", 0.9 }, { "good", 0.5 }, { "wonderful", 0.85 },
            { "helpful", 0.6 }, { "thank", 0.5 }, { "thanks", 0.5 }, { "resolved", 0.7 },
            { "appreciated", 0.7 }, { "fantastic", 0.9 }
        };

        double score = 0.0;
        int matches = 0;

        foreach (var kvp in negativeKeywords)
        {
            if (lower.Contains(kvp.Key))
            {
                score += kvp.Value;
                matches++;
            }
        }

        foreach (var kvp in positiveKeywords)
        {
            if (lower.Contains(kvp.Key))
            {
                score += kvp.Value;
                matches++;
            }
        }

        if (matches == 0)
        {
            // Default baseline for general support tickets
            return Task.FromResult(-0.2);
        }

        double normalized = Math.Clamp(score / Math.Max(1, matches), -1.0, 1.0);
        return Task.FromResult(normalized);
    }

    public Task<string> DetermineSeverityTierAsync(string text, double sentimentScore, TicketPriority priority)
    {
        var lower = text.ToLowerInvariant();

        if (lower.Contains("emergency") || lower.Contains("unsafe") || lower.Contains("hospital") || lower.Contains("police") || priority == TicketPriority.Critical)
        {
            return Task.FromResult("Tier_3_Critical");
        }

        if (sentimentScore <= -0.5 || lower.Contains("delay") || lower.Contains("cancelled") || lower.Contains("refund") || priority == TicketPriority.High)
        {
            return Task.FromResult("Tier_2_High");
        }

        return Task.FromResult("Tier_1_Low");
    }

    public Task<(bool isEligible, decimal amount, string reason)> EvaluateGoodwillEligibilityAsync(SupportTicket ticket, string severityTier)
    {
        // Business Rule / Tool Check: Tour disruptions, high severity claims, or delays automatically qualify for $50 Goodwill Voucher
        var category = ticket.Category?.ToLowerInvariant() ?? "";
        var desc = ticket.Description.ToLowerInvariant();

        if (severityTier == "Tier_3_Critical")
        {
            return Task.FromResult((true, 100.00m, "Expedited critical disruption compensation voucher"));
        }

        if (severityTier == "Tier_2_High" || category.Contains("delay") || category.Contains("quality") || desc.Contains("delay") || desc.Contains("disruption") || desc.Contains("cancelled"))
        {
            return Task.FromResult((true, 50.00m, "Standard $50 goodwill compensation for tour disruption / delay"));
        }

        return Task.FromResult((false, 0m, "Standard customer support handling"));
    }
}
