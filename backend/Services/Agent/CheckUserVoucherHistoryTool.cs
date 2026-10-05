using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;

namespace Travyle.Api.Services.Agent;

public record CheckVoucherHistoryInput(
    Guid UserId,
    int LookbackDays = 30
);

public record CheckVoucherHistoryOutput(
    Guid UserId,
    int RecentVoucherCount,
    decimal TotalRecentAmount,
    bool HasRecentGoodwillVoucher,
    DateTime? LastVoucherDate
);

public interface ICheckUserVoucherHistoryTool
{
    Task<CheckVoucherHistoryOutput> ExecuteAsync(CheckVoucherHistoryInput input, CancellationToken cancellationToken = default);
}

public class CheckUserVoucherHistoryTool : ICheckUserVoucherHistoryTool
{
    private readonly TravyleDbContext _db;

    public CheckUserVoucherHistoryTool(TravyleDbContext db)
    {
        _db = db;
    }

    public async Task<CheckVoucherHistoryOutput> ExecuteAsync(CheckVoucherHistoryInput input, CancellationToken cancellationToken = default)
    {
        if (input.UserId == Guid.Empty)
        {
            return new CheckVoucherHistoryOutput(Guid.Empty, 0, 0m, false, null);
        }

        // Input validation (least privilege): bounded lookback window.
        var days = input.LookbackDays > 0 ? Math.Min(input.LookbackDays, 90) : 30;
        var cutoff = DateTime.UtcNow.AddDays(-days);

        var recentVouchers = await _db.Vouchers
            .Where(v => v.UserId == input.UserId && !v.IsDeleted && v.CreatedAt >= cutoff)
            .ToListAsync(cancellationToken);

        var count = recentVouchers.Count;
        var totalAmount = recentVouchers.Sum(v => v.Amount);
        var hasRecent = count > 0;
        var lastDate = recentVouchers.OrderByDescending(v => v.CreatedAt).FirstOrDefault()?.CreatedAt;

        return new CheckVoucherHistoryOutput(
            input.UserId,
            count,
            totalAmount,
            hasRecent,
            lastDate
        );
    }
}
