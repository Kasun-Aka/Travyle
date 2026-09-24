namespace Travyle.Api.DTOs.Operations;

// ── TourActivity ────────────────────────────────────────
public record CreateTourActivityDto(
    Guid BookingScheduleId,
    string ActivityName,
    DateTime ScheduledTime,
    string Location,
    string Status = "Scheduled"
);

public record TourActivityResponseDto(
    Guid Id,
    Guid BookingScheduleId,
    string ActivityName,
    DateTime ScheduledTime,
    string Location,
    string Status
);

// ── GuideAssignment ─────────────────────────────────────
public record GuideAssignmentResponseDto(
    Guid Id,
    Guid BookingScheduleId,
    Guid GuideUserId,
    string? GuideFullName,
    string Status,
    DateTime AssignedAt
);

public record UpdateGuideAssignmentDto(
    Guid? GuideUserId,
    string? Status
);

// ── RouteLog ────────────────────────────────────────────
public record CreateRouteLogDto(
    Guid BookingScheduleId,
    Guid RecordedBy,
    decimal Latitude,
    decimal Longitude
);

public record RouteLogResponseDto(
    Guid Id,
    Guid BookingScheduleId,
    Guid RecordedBy,
    decimal Latitude,
    decimal Longitude,
    DateTime Timestamp
);

// ── DisruptionAlert ─────────────────────────────────────
public record DisruptionAlertResponseDto(
    Guid Id,
    Guid BookingScheduleId,
    string Type,
    string Severity,
    string Description,
    DateTime TriggeredAt,
    DateTime? ResolvedAt
);

// ── Route Optimization ──────────────────────────────────
public record RouteOptimizationRequestDto(
    Guid BookingScheduleId,
    List<WaypointDto> Waypoints
);

public record WaypointDto(
    string Name,
    decimal Latitude,
    decimal Longitude
);

public record RouteOptimizationResponseDto(
    List<WaypointDto> OptimizedOrder,
    double EstimatedDistanceKm
);

// ── Pagination ──────────────────────────────────────────
public record PaginatedResult<T>(
    List<T> Items,
    int TotalCount,
    int Page,
    int PageSize
);
