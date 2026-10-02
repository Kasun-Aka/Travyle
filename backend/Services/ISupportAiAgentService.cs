using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Services;

public record AgentTicketInput(
    Guid TicketId,
    Guid UserId,
    string Title,
    string Description,
    string Category,
    TicketPriority Priority,
    Guid? BookingId = null,
    Guid? TourId = null
);

public record AgentVoucherProposal(
    decimal Amount,
    string Reason,
    string Code
);

public record AgentTriageOutput(
    Guid TicketId,
    double SentimentScore,
    string SeverityTier,
    string Reasoning,
    AgentVoucherProposal? ProposedVoucher,
    bool IsFallbackUsed,
    List<AuditLogResponseDto> AuditLogs
);

public interface ISupportQualityAgent
{
    Task<AgentTriageOutput> ProcessTicketAsync(AgentTicketInput input, CancellationToken cancellationToken = default);
}

public interface ISupportAiAgentService : ISupportQualityAgent
{
    Task<AutoResolveResultDto> TriageTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task<double> CalculateSentimentScoreAsync(string text);
    Task<string> DetermineSeverityTierAsync(string text, double sentimentScore, TicketPriority priority);
    Task<(bool isEligible, decimal amount, string reason)> EvaluateGoodwillEligibilityAsync(SupportTicket ticket, string severityTier);
}
