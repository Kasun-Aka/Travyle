using Travyle.Api.DTOs.Operations;
using Travyle.Api.Models;
using Travyle.Api.Repositories;

namespace Travyle.Api.Services;

public class OperationsService : IOperationsService
{
    private readonly IOperationsRepository _repo;

    public OperationsService(IOperationsRepository repo)
    {
        _repo = repo;
    }

    // ── TourActivities ──────────────────────────────────────

    public async Task<TourActivityResponseDto> CreateTourActivityAsync(CreateTourActivityDto dto)
    {
        var entity = new TourActivity
        {
            BookingScheduleId = dto.BookingScheduleId,
            ActivityName = dto.ActivityName,
            ScheduledTime = dto.ScheduledTime,
            Location = dto.Location,
            Status = dto.Status
        };

        var created = await _repo.CreateTourActivityAsync(entity);
        return MapToDto(created);
    }

    public async Task<List<TourActivityResponseDto>> GetActivitiesByScheduleIdAsync(Guid bookingScheduleId)
    {
        var items = await _repo.GetActivitiesByScheduleIdAsync(bookingScheduleId);
        return items.Select(MapToDto).ToList();
    }

    // ── GuideAssignments ────────────────────────────────────

    public async Task<GuideAssignmentResponseDto?> UpdateGuideAssignmentAsync(
        Guid id, UpdateGuideAssignmentDto dto)
    {
        var existing = await _repo.GetGuideAssignmentByIdAsync(id);
        if (existing is null) return null;

        if (dto.GuideUserId.HasValue)
            existing.GuideUserId = dto.GuideUserId.Value;

        if (!string.IsNullOrWhiteSpace(dto.Status))
            existing.Status = dto.Status;

        var updated = await _repo.UpdateGuideAssignmentAsync(existing);
        return MapToDto(updated);
    }

    public async Task<PaginatedResult<GuideAssignmentResponseDto>> GetAvailableGuidesAsync(
        string? location, string? language, int page, int pageSize)
    {
        var (items, totalCount) = await _repo.GetAvailableGuidesAsync(location, language, page, pageSize);
        var dtos = items.Select(MapToDto).ToList();
        return new PaginatedResult<GuideAssignmentResponseDto>(dtos, totalCount, page, pageSize);
    }

    // ── RouteLogs ───────────────────────────────────────────

    public async Task<RouteLogResponseDto> CreateRouteLogAsync(CreateRouteLogDto dto)
    {
        var entity = new RouteLog
        {
            BookingScheduleId = dto.BookingScheduleId,
            RecordedBy = dto.RecordedBy,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude
        };

        var created = await _repo.CreateRouteLogAsync(entity);
        return MapToDto(created);
    }

    public async Task<List<RouteLogResponseDto>> GetRouteLogsByTourIdAsync(Guid tourId)
    {
        var items = await _repo.GetRouteLogsByTourIdAsync(tourId);
        return items.Select(MapToDto).ToList();
    }

    // ── DisruptionAlerts ────────────────────────────────────

    public async Task<List<DisruptionAlertResponseDto>> GetActiveAlertsAsync(Guid? bookingScheduleId)
    {
        var items = await _repo.GetActiveAlertsAsync(bookingScheduleId);
        return items.Select(MapToDto).ToList();
    }

    // ── Route Optimization (Nearest-Neighbour TSP stub) ─────

    public Task<RouteOptimizationResponseDto> ReorderRouteOptimizationAsync(RouteOptimizationRequestDto dto)
    {
        var waypoints = dto.Waypoints.ToList();
        if (waypoints.Count <= 1)
        {
            return Task.FromResult(new RouteOptimizationResponseDto(waypoints, 0));
        }

        // Simple nearest-neighbour heuristic
        var optimized = new List<WaypointDto> { waypoints[0] };
        var remaining = waypoints.Skip(1).ToList();
        double totalDistance = 0;

        while (remaining.Count > 0)
        {
            var current = optimized.Last();
            var nearest = remaining
                .OrderBy(w => HaversineKm(
                    (double)current.Latitude, (double)current.Longitude,
                    (double)w.Latitude, (double)w.Longitude))
                .First();

            totalDistance += HaversineKm(
                (double)current.Latitude, (double)current.Longitude,
                (double)nearest.Latitude, (double)nearest.Longitude);

            optimized.Add(nearest);
            remaining.Remove(nearest);
        }

        return Task.FromResult(new RouteOptimizationResponseDto(optimized, Math.Round(totalDistance, 2)));
    }

    // ── Private helpers ─────────────────────────────────────

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371; // Earth radius in km
        var dLat = ToRad(lat2 - lat1);
        var dLon = ToRad(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double ToRad(double deg) => deg * Math.PI / 180;

    private static TourActivityResponseDto MapToDto(TourActivity a) =>
        new(a.Id, a.BookingScheduleId, a.ActivityName, a.ScheduledTime, a.Location, a.Status);

    private static GuideAssignmentResponseDto MapToDto(GuideAssignment ga) =>
        new(ga.Id, ga.BookingScheduleId, ga.GuideUserId, ga.Guide?.FullName, ga.Status, ga.AssignedAt);

    private static RouteLogResponseDto MapToDto(RouteLog r) =>
        new(r.Id, r.BookingScheduleId, r.RecordedBy, r.Latitude, r.Longitude, r.Timestamp);

    private static DisruptionAlertResponseDto MapToDto(DisruptionAlert d) =>
        new(d.Id, d.BookingScheduleId, d.Type, d.Severity, d.Description, d.TriggeredAt, d.ResolvedAt);
}
