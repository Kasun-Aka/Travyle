using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/admin")]
public class AdminController : ControllerBase
{
    private readonly TravyleDbContext _db;

    public AdminController(TravyleDbContext db)
    {
        _db = db;
    }

    // GET /api/admin/staff
    [HttpGet("staff")]
    public async Task<IActionResult> GetStaff()
    {
        var staff = await _db.Users
            .Where(u => u.Role == "Guide" || u.Role == "Local Guide" || u.Role == "Tour Operator" || u.Role == "Operator")
            .Select(u => new
            {
                Id = u.Id,
                FirebaseUid = u.FirebaseUid,
                Email = u.Email,
                FullName = u.FullName,
                Role = u.Role,
                CreatedAt = u.CreatedAt
            })
            .ToListAsync();

        return Ok(staff);
    }

    [HttpGet("live-operations")]
    public async Task<IActionResult> GetLiveOperations()
    {
        try
        {
            // Stats
            var toursInProgress = await _db.TourActivities
                .Where(a => a.Status == "InProgress")
                .Select(a => a.BookingScheduleId)
                .Distinct()
                .CountAsync();

            var flagged = await _db.DisruptionAlerts
                .Where(d => d.ResolvedAt == null)
                .Select(d => d.BookingScheduleId)
                .Distinct()
                .CountAsync();

            var guidesOnDuty = await _db.GuideAssignments
                .Where(ga => ga.Status == "Active")
                .CountAsync();

            var guidesAvailable = await _db.Users
                .Where(u => u.Role == "Local Guide" && !_db.GuideAssignments.Any(ga => ga.GuideUserId == u.Id && ga.Status == "Active"))
                .CountAsync();

            var activeAlertsCount = await _db.DisruptionAlerts
                .CountAsync(d => d.ResolvedAt == null);

            var totalPings = await _db.RouteLogs.CountAsync();

            var stats = new
            {
                toursInProgress,
                flagged,
                guidesOnDuty,
                guidesAvailable,
                activeAlerts = activeAlertsCount,
                avgScheduleDrift = "+0 min",
                totalPings
            };

            // Active Tours — enriched with guide name, progress, last ping
            var activeSchedules = await _db.BookingSchedules
                .Include(bs => bs.TourActivities)
                .Include(bs => bs.RouteLogs)
                .Include(bs => bs.DisruptionAlerts)
                .Include(bs => bs.GuideAssignments)
                    .ThenInclude(ga => ga.Guide)
                .Where(bs => bs.TourActivities.Any(ta => ta.Status == "InProgress"))
                .ToListAsync();

            var activeToursData = activeSchedules.Select(bs =>
            {
                var totalStops = bs.TourActivities.Count;
                var completedStops = bs.TourActivities.Count(a => a.Status == "Completed");
                var inProgressStops = bs.TourActivities.Count(a => a.Status == "InProgress");
                var doneStops = completedStops + inProgressStops;
                var percent = totalStops > 0 ? (int)Math.Round(100.0 * doneStops / totalStops) : 0;
                var lastLog = bs.RouteLogs.OrderByDescending(r => r.Timestamp).FirstOrDefault();
                var guide = bs.GuideAssignments.FirstOrDefault()?.Guide;
                var hasAlert = bs.DisruptionAlerts.Any(d => d.ResolvedAt == null);

                return new
                {
                    id = bs.Id,
                    name = bs.DestinationTitle,
                    title = bs.DestinationTitle,
                    guide = guide?.FullName ?? bs.GuideName,
                    location = bs.Location,
                    progress = $"{doneStops}/{totalStops}",
                    progressPercent = percent,
                    lastPing = lastLog != null ? lastLog.Timestamp.ToString("HH:mm") : "—",
                    currentLocation = lastLog != null
                        ? new { lat = lastLog.Latitude, lon = lastLog.Longitude }
                        : (object?)null,
                    status = hasAlert ? "Delayed" : "On Time"
                };
            }).ToList();

            // Alerts — enriched with title, tour info, proposal text
            var alertsRaw = await _db.DisruptionAlerts
                .Include(d => d.BookingSchedule)
                .Where(d => d.ResolvedAt == null)
                .ToListAsync();

            var alertsData = alertsRaw.Select(d => new
            {
                id = d.Id,
                title = $"{d.Type} Alert — {d.BookingSchedule?.DestinationTitle ?? "Unknown Tour"}",
                type = d.Type,
                tour = d.BookingSchedule?.DestinationTitle ?? "Unknown",
                tourId = d.BookingScheduleId,
                source = d.Type == "Weather" ? "OpenWeatherMap" : "Traffic API",
                description = d.Description,
                severity = d.Severity,
                time = d.TriggeredAt.ToString("HH:mm"),
                proposal = d.Severity == "High"
                    ? $"Re-order stops to avoid {d.Type.ToLower()} disruption. TSP solver suggests postponing affected activities."
                    : d.Severity == "Medium"
                        ? $"Consider rescheduling activities. {d.Description}"
                        : $"Monitor situation. {d.Description}",
                status = "Pending"
            }).ToList();

            return Ok(new { stats, activeTours = activeToursData, alerts = alertsData });
        }
        catch (System.Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

    // GET /api/admin/route-log/{scheduleId}
    [HttpGet("route-log/{scheduleId}")]
    public async Task<IActionResult> GetRouteLog(Guid scheduleId)
    {
        var schedule = await _db.BookingSchedules
            .Include(bs => bs.TourActivities)
            .Include(bs => bs.GuideAssignments).ThenInclude(ga => ga.Guide)
            .Include(bs => bs.DisruptionAlerts)
            .FirstOrDefaultAsync(bs => bs.Id == scheduleId);

        if (schedule == null) return NotFound();

        var guide = schedule.GuideAssignments.FirstOrDefault()?.Guide;
        var hasAlert = schedule.DisruptionAlerts.Any(d => d.ResolvedAt == null);

        var stops = schedule.TourActivities
            .OrderBy(a => a.ScheduledTime)
            .Select(a => new
            {
                name = a.ActivityName,
                location = a.Location,
                time = a.ScheduledTime.ToString("HH:mm"),
                status = a.Status
            })
            .ToList();

        return Ok(new
        {
            tourName = schedule.DestinationTitle,
            guideName = guide?.FullName ?? schedule.GuideName,
            hasDisruption = hasAlert,
            stops
        });
    }

    [HttpGet("guide-matrix")]
    public async Task<IActionResult> GetGuideMatrix()
    {
        var guides = await _db.Users
            .Where(u => u.Role == "Guide" || u.Role == "Local Guide" || u.Role == "Suspended")
            .Select(u => new
            {
                Id = u.Id,
                Name = u.FullName,
                Meta = "Verified   " + u.Email,
                AssignedTours = _db.GuideAssignments.Count(ga => ga.GuideUserId == u.Id),
                Status = _db.GuideAssignments.Any(ga => ga.GuideUserId == u.Id && ga.Status == "Active") ? "Active" : "Available"
            })
            .ToListAsync();

        var data = new
        {
            guides = guides.Select(g => new
            {
                id = g.Id,
                name = g.Name,
                meta = g.Meta,
                schedule = new[] { "-", "TOUR-5510", "-", "SLOT-8841", "SLOT-8841", "-" }
            }),
            roster = guides.Select(g => new
            {
                name = g.Name,
                meta = g.Meta,
                languages = new[] { "EN", "SI" },
                verification = "Verified",
                load = $"{g.AssignedTours} tours",
                status = g.Status
            }),
            agentSuggestion = new { title = "Operations Agent", message = "No urgent guide reassignments needed at this time." }
        };

        return Ok(data);
    }

    [HttpPut("staff/{id}/role")]
    public async Task<IActionResult> UpdateRole(Guid id, [FromBody] UpdateRoleRequest req)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = req.Role;
        await _db.SaveChangesAsync();
        return Ok();
    }

    [HttpPut("staff/{id}/suspend")]
    public async Task<IActionResult> SuspendStaff(Guid id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null) return NotFound();
        user.Role = "Suspended";
        await _db.SaveChangesAsync();
        return Ok();
    }
}

public class UpdateRoleRequest
{
    public string Role { get; set; } = string.Empty;
}
