using Travyle.Api.Models;

namespace Travyle.Api.Repositories;

public interface IOperationsRepository
{
    // TourActivities
    Task<TourActivity> CreateTourActivityAsync(TourActivity activity);
    Task<List<TourActivity>> GetActivitiesByScheduleIdAsync(Guid bookingScheduleId);

    // GuideAssignments
    Task<GuideAssignment?> GetGuideAssignmentByIdAsync(Guid id);
    Task<GuideAssignment> UpdateGuideAssignmentAsync(GuideAssignment assignment);
    Task<(List<GuideAssignment> Items, int TotalCount)> GetAvailableGuidesAsync(
        string? location, string? language, int page, int pageSize);

    // RouteLogs
    Task<RouteLog> CreateRouteLogAsync(RouteLog log);
    Task<List<RouteLog>> GetRouteLogsByTourIdAsync(Guid bookingScheduleId);

    // DisruptionAlerts
    Task<List<DisruptionAlert>> GetActiveAlertsAsync(Guid? bookingScheduleId);
}
