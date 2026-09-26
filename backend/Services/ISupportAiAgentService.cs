using Travyle.Api.DTOs;
using Travyle.Api.Models;

namespace Travyle.Api.Services;

public interface ISupportAiAgentService
{
    Task<AutoResolveResultDto> TriageTicketAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task<double> CalculateSentimentScoreAsync(string text);
    Task<string> DetermineSeverityTierAsync(string text, double sentimentScore, TicketPriority priority);
    Task<(bool isEligible, decimal amount, string reason)> EvaluateGoodwillEligibilityAsync(SupportTicket ticket, string severityTier);
}
