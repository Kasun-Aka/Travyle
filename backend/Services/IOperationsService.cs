using Travyle.Api.DTOs.Operations;

namespace Travyle.Api.Services;

public interface IOperationsService
{
    // TourActivities
    Task<TourActivityResponseDto> CreateTourActivityAsync(CreateTourActivityDto dto);
    Task<List<TourActivityResponseDto>> GetActivitiesByScheduleIdAsync(Guid bookingScheduleId);

    // GuideAssignments
    Task<GuideAssignmentResponseDto?> UpdateGuideAssignmentAsync(Guid id, UpdateGuideAssignmentDto dto);
    Task<PaginatedResult<GuideAssignmentResponseDto>> GetAvailableGuidesAsync(
        string? location, string? language, int page, int pageSize);

    // RouteLogs
    Task<RouteLogResponseDto> CreateRouteLogAsync(CreateRouteLogDto dto);
    Task<List<RouteLogResponseDto>> GetRouteLogsByTourIdAsync(Guid tourId);

    // DisruptionAlerts
    Task<List<DisruptionAlertResponseDto>> GetActiveAlertsAsync(Guid? bookingScheduleId);
    Task<DisruptionAlertResponseDto> CreateDisruptionAlertAsync(CreateDisruptionAlertDto dto);

    // Route Optimization
    Task<RouteOptimizationResponseDto> ReorderRouteOptimizationAsync(RouteOptimizationRequestDto dto);

    // AI Agent Integration
    Task<MonitorOperationsResponseDto?> MonitorOperationsAsync(MonitorOperationsRequestDto dto);
}
