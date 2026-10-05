using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;

namespace Travyle.Api.Services.Agent;

public record CheckReviewHistoryInput(
    Guid UserId,
    int LookbackDays = 180
);

public record CheckReviewHistoryOutput(
    Guid UserId,
    int TotalReviewsCount,
    double AverageRating,
    bool HasLowRatingPattern,
    DateTime? LastReviewDate
);

public interface ICheckUserReviewHistoryTool
{
    Task<CheckReviewHistoryOutput> ExecuteAsync(CheckReviewHistoryInput input, CancellationToken cancellationToken = default);
}

public class CheckUserReviewHistoryTool : ICheckUserReviewHistoryTool
{
    private readonly TravyleDbContext _db;

    public CheckUserReviewHistoryTool(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<CheckReviewHistoryOutput> ExecuteAsync(CheckReviewHistoryInput input, CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            return new CheckReviewHistoryOutput(Guid.Empty, 0, 0.0, false, null);
        }

        var days = input.LookbackDays > 0 ? Math.Min(input.LookbackDays, 365) : 180;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var reviews = await _db.CustomerReviews
            .Where(r => r.UserId == input.UserId && r.CreatedAt >= cutoff)
            .ToListAsync(cancellationToken);

        var count = reviews.Count;
        double avgRating = count > 0 ? reviews.Average(r => (double)r.Rating) : 0.0;
        bool hasLowRatingPattern = count >= 1 && avgRating < 2.5;
        var lastDate = reviews.OrderByDescending(r => r.CreatedAt).FirstOrDefault()?.CreatedAt;

        return new CheckReviewHistoryOutput(
            input.UserId,
            count,
            avgRating,
            hasLowRatingPattern,
            lastDate
        );
    }
}
