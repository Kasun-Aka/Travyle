using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Repositories;

public class OperationsRepository : IOperationsRepository
{
    private readonly TravyleDbContext _db;

    public OperationsRepository(TravyleDbContext db)
    {
        _db = db;
    }

    // ── TourActivities ──────────────────────────────────────

    public async Task<TourActivity> CreateTourActivityAsync(TourActivity activity)
    {
        _db.TourActivities.Add(activity);
        await _db.SaveChangesAsync();
        return activity;
    }

    public async Task<List<TourActivity>> GetActivitiesByScheduleIdAsync(Guid bookingScheduleId)
    {
        return await _db.TourActivities
            .Where(a => a.BookingScheduleId == bookingScheduleId)
            .OrderBy(a => a.ScheduledTime)
            .ToListAsync();
    }

    // ── GuideAssignments ────────────────────────────────────

    public async Task<GuideAssignment?> GetGuideAssignmentByIdAsync(Guid id)
    {
        return await _db.GuideAssignments
            .Include(ga => ga.Guide)
            .FirstOrDefaultAsync(ga => ga.Id == id);
    }

    public async Task<GuideAssignment> UpdateGuideAssignmentAsync(GuideAssignment assignment)
    {
        _db.GuideAssignments.Update(assignment);
        await _db.SaveChangesAsync();
        return assignment;
    }

    public async Task<(List<GuideAssignment> Items, int TotalCount)> GetAvailableGuidesAsync(
        string? location, string? language, int page, int pageSize)
    {
        var query = _db.GuideAssignments
            .Include(ga => ga.Guide)
            .Where(ga => ga.Status == "Assigned" || ga.Status == "Active")
            .AsQueryable();

        // Future: filter by guide's location / language when those fields
        // are added to the User or a GuideProfile model.

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(ga => ga.AssignedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    // ── RouteLogs ───────────────────────────────────────────

    public async Task<RouteLog> CreateRouteLogAsync(RouteLog log)
    {
        _db.RouteLogs.Add(log);
        await _db.SaveChangesAsync();
        return log;
    }

    public async Task<List<RouteLog>> GetRouteLogsByTourIdAsync(Guid bookingScheduleId)
    {
        return await _db.RouteLogs
            .Where(r => r.BookingScheduleId == bookingScheduleId)
            .OrderBy(r => r.Timestamp)
            .ToListAsync();
    }

    // ── DisruptionAlerts ────────────────────────────────────

    public async Task<List<DisruptionAlert>> GetActiveAlertsAsync(Guid? bookingScheduleId)
    {
        var query = _db.DisruptionAlerts
            .Where(d => d.ResolvedAt == null)
            .AsQueryable();

        if (bookingScheduleId.HasValue)
        {
            query = query.Where(d => d.BookingScheduleId == bookingScheduleId.Value);
        }

        return await query
            .OrderByDescending(d => d.TriggeredAt)
            .ToListAsync();
    }
}
