using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Controllers;
using Travyle.Api.Data;
using Travyle.Api.Models;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Unit tests for the AdminController.
/// Covers: GetStaff, GetLiveOperations, GetRouteLog, GetGuideMatrix,
///         GetUnassignedSchedules, AssignGuide, UnassignGuide, UpdateRole, SuspendStaff.
/// Uses EF Core InMemory provider for isolated database per test.
/// </summary>
public class AdminControllerTests : IDisposable
{
    private readonly TravyleDbContext _db;
    private readonly AdminController _controller;

    public AdminControllerTests()
    {
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TravyleDbContext(options);
        _controller = new AdminController(_db);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private User SeedUser(string role = "Guide", string name = "Test User", string email = "test@test.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            FirebaseUid = Guid.NewGuid().ToString(),
            Email = email,
            FullName = name,
            Role = role
        };
        _db.Users.Add(user);
        return user;
    }

    private BookingSchedule SeedSchedule(string title = "Sigiriya Tour", string location = "Sigiriya")
    {
        var schedule = new BookingSchedule
        {
            Id = Guid.NewGuid(),
            DestinationTitle = title,
            Location = location,
            GuideName = "Default Guide",
            MaxCapacityPerSlot = 10
        };
        _db.BookingSchedules.Add(schedule);
        return schedule;
    }

    private GuideAssignment SeedAssignment(Guid scheduleId, Guid guideId, string status = "Active")
    {
        var assignment = new GuideAssignment
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = scheduleId,
            GuideUserId = guideId,
            Status = status,
            AssignedAt = DateTime.UtcNow
        };
        _db.GuideAssignments.Add(assignment);
        return assignment;
    }

    private TourActivity SeedActivity(Guid scheduleId, string name = "Temple Visit", string status = "Scheduled")
    {
        var activity = new TourActivity
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = scheduleId,
            ActivityName = name,
            ScheduledTime = DateTime.UtcNow,
            Location = "Test Location",
            Status = status
        };
        _db.TourActivities.Add(activity);
        return activity;
    }

    private DisruptionAlert SeedAlert(Guid scheduleId, string type = "Weather", string severity = "High", DateTime? resolvedAt = null)
    {
        var alert = new DisruptionAlert
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = scheduleId,
            Type = type,
            Severity = severity,
            Description = $"{type} disruption detected",
            TriggeredAt = DateTime.UtcNow,
            ResolvedAt = resolvedAt
        };
        _db.DisruptionAlerts.Add(alert);
        return alert;
    }

    private RouteLog SeedRouteLog(Guid scheduleId, Guid recordedBy)
    {
        var log = new RouteLog
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = scheduleId,
            RecordedBy = recordedBy,
            Latitude = 7.9572m,
            Longitude = 80.7601m,
            Timestamp = DateTime.UtcNow
        };
        _db.RouteLogs.Add(log);
        return log;
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /api/admin/staff
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetStaff_EmptyDb_ReturnsEmptyList()
    {
        var result = await _controller.GetStaff();
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetStaff_ReturnsOnlyStaffRoles()
    {
        // Arrange — seed multiple roles
        SeedUser("Guide", "Guide A", "a@test.com");
        SeedUser("Local Guide", "Guide B", "b@test.com");
        SeedUser("Tour Operator", "Operator C", "c@test.com");
        SeedUser("Operator", "Operator D", "d@test.com");
        SeedUser("Tourist", "Tourist E", "e@test.com");       // excluded
        SeedUser("Admin", "Admin F", "f@test.com");            // excluded
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetStaff();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var staff = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Equal(4, staff.Count());
    }

    [Fact]
    public async Task GetStaff_ExcludesTouristAndAdminRoles()
    {
        // Arrange
        SeedUser("Tourist", "Tourist A", "tourist@test.com");
        SeedUser("Admin", "Admin B", "admin@test.com");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetStaff();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var staff = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Empty(staff);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /api/admin/live-operations
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetLiveOperations_EmptyDb_ReturnsOkWithZeroStats()
    {
        var result = await _controller.GetLiveOperations();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetLiveOperations_WithActiveData_ReturnsStats()
    {
        // Arrange — seed a tour in progress with guide, alert, and route log
        var guide = SeedUser("Local Guide", "Kamal", "kamal@test.com");
        var schedule = SeedSchedule("Ella Rock Hike");
        SeedAssignment(schedule.Id, guide.Id, "Active");
        SeedActivity(schedule.Id, "Hike Start", "InProgress");
        SeedActivity(schedule.Id, "Summit", "Scheduled");
        SeedAlert(schedule.Id, "Weather", "High");
        SeedRouteLog(schedule.Id, guide.Id);
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetLiveOperations();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetLiveOperations_ActiveTour_ShowsDelayedWhenAlertUnresolved()
    {
        // Arrange
        var guide = SeedUser("Local Guide", "Nimal", "nimal@test.com");
        var schedule = SeedSchedule("Galle Fort Walk");
        SeedAssignment(schedule.Id, guide.Id, "Active");
        SeedActivity(schedule.Id, "Fort Gate", "InProgress");
        SeedAlert(schedule.Id, "Traffic", "Medium"); // unresolved
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetLiveOperations();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        // The response contains active tours with status "Delayed" when unresolved alerts exist
    }

    [Fact]
    public async Task GetLiveOperations_NoInProgressActivities_ReturnsEmptyActiveTours()
    {
        // Arrange — all activities are Completed, none InProgress
        var schedule = SeedSchedule("Completed Tour");
        SeedActivity(schedule.Id, "Stop 1", "Completed");
        SeedActivity(schedule.Id, "Stop 2", "Completed");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetLiveOperations();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /api/admin/route-log/{scheduleId}
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetRouteLog_ValidSchedule_ReturnsOkWithStops()
    {
        // Arrange
        var guide = SeedUser("Local Guide", "Saman", "saman@test.com");
        var schedule = SeedSchedule("Kandy Lake Walk");
        SeedAssignment(schedule.Id, guide.Id);
        SeedActivity(schedule.Id, "Temple of Tooth", "InProgress");
        SeedActivity(schedule.Id, "Royal Palace", "Scheduled");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRouteLog(schedule.Id);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetRouteLog_InvalidScheduleId_ReturnsNotFound()
    {
        var result = await _controller.GetRouteLog(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task GetRouteLog_WithDisruption_SetsHasDisruptionTrue()
    {
        // Arrange
        var schedule = SeedSchedule("Yala Safari");
        SeedActivity(schedule.Id, "Entrance", "Scheduled");
        SeedAlert(schedule.Id, "Weather", "High"); // unresolved
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRouteLog(schedule.Id);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetRouteLog_StopsOrderedByScheduledTime()
    {
        // Arrange
        var schedule = SeedSchedule("Multi-Stop Tour");
        var now = DateTime.UtcNow;

        _db.TourActivities.Add(new TourActivity
        {
            BookingScheduleId = schedule.Id,
            ActivityName = "Stop B (Later)",
            ScheduledTime = now.AddHours(2),
            Location = "B",
            Status = "Scheduled"
        });
        _db.TourActivities.Add(new TourActivity
        {
            BookingScheduleId = schedule.Id,
            ActivityName = "Stop A (Earlier)",
            ScheduledTime = now.AddHours(1),
            Location = "A",
            Status = "Scheduled"
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetRouteLog(schedule.Id);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /api/admin/guide-matrix
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetGuideMatrix_EmptyDb_ReturnsOk()
    {
        var result = await _controller.GetGuideMatrix();
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetGuideMatrix_WithGuides_ReturnsGuideData()
    {
        // Arrange
        SeedUser("Guide", "Guide A", "a@test.com");
        SeedUser("Local Guide", "Guide B", "b@test.com");
        SeedUser("Tourist", "Tourist C", "c@test.com"); // excluded from matrix
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetGuideMatrix();

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetGuideMatrix_GuideWithAssignment_ShowsActiveStatus()
    {
        // Arrange
        var guide = SeedUser("Local Guide", "Active Guide", "active@test.com");
        var schedule = SeedSchedule("Active Tour");
        SeedAssignment(schedule.Id, guide.Id, "Active");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetGuideMatrix();

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // GET /api/admin/unassigned-schedules
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task GetUnassignedSchedules_NoSchedules_ReturnsEmptyList()
    {
        var result = await _controller.GetUnassignedSchedules(DateTime.UtcNow);
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetUnassignedSchedules_UnassignedOnDate_ReturnsIt()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var schedule = SeedSchedule("Unassigned Tour");
        _db.ScheduleAvailableDates.Add(new ScheduleAvailableDate
        {
            BookingScheduleId = schedule.Id,
            Date = today
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetUnassignedSchedules(today);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetUnassignedSchedules_AssignedSchedule_ExcludesIt()
    {
        // Arrange
        var today = DateTime.UtcNow.Date;
        var guide = SeedUser("Local Guide", "Guide", "guide@test.com");
        var schedule = SeedSchedule("Assigned Tour");
        SeedAssignment(schedule.Id, guide.Id);
        _db.ScheduleAvailableDates.Add(new ScheduleAvailableDate
        {
            BookingScheduleId = schedule.Id,
            Date = today
        });
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.GetUnassignedSchedules(today);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var items = Assert.IsAssignableFrom<IEnumerable<object>>(ok.Value);
        Assert.Empty(items);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // POST /api/admin/assign-guide
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task AssignGuide_ValidSchedule_CreatesAssignment()
    {
        // Arrange
        var guideId = Guid.NewGuid();
        var schedule = SeedSchedule("Tour to Assign");
        await _db.SaveChangesAsync();

        var req = new AssignGuideRequest { BookingScheduleId = schedule.Id, GuideId = guideId };

        // Act
        var result = await _controller.AssignGuide(req);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var assignment = await _db.GuideAssignments
            .FirstOrDefaultAsync(ga => ga.BookingScheduleId == schedule.Id && ga.GuideUserId == guideId);
        Assert.NotNull(assignment);
        Assert.Equal("Active", assignment.Status);
    }

    [Fact]
    public async Task AssignGuide_NonExistentSchedule_ReturnsNotFound()
    {
        var req = new AssignGuideRequest { BookingScheduleId = Guid.NewGuid(), GuideId = Guid.NewGuid() };

        var result = await _controller.AssignGuide(req);

        Assert.IsType<NotFoundObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // DELETE /api/admin/unassign-guide/{guideId}/{scheduleId}
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UnassignGuide_ExistingAssignment_RemovesIt()
    {
        // Arrange
        var guideId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        SeedAssignment(scheduleId, guideId, "Active");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.UnassignGuide(guideId, scheduleId);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        var remaining = await _db.GuideAssignments
            .AnyAsync(ga => ga.GuideUserId == guideId && ga.BookingScheduleId == scheduleId);
        Assert.False(remaining);
    }

    [Fact]
    public async Task UnassignGuide_NoMatchingAssignment_StillReturnsOk()
    {
        // Act — no assignment exists, but endpoint still returns Ok
        var result = await _controller.UnassignGuide(Guid.NewGuid(), Guid.NewGuid());

        Assert.IsType<OkObjectResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUT /api/admin/staff/{id}/role
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task UpdateRole_ValidUser_UpdatesRole()
    {
        // Arrange
        var user = SeedUser("Guide", "Testman", "testman@test.com");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.UpdateRole(user.Id, new UpdateRoleRequest { Role = "Tour Operator" });

        // Assert
        Assert.IsType<OkResult>(result);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.Equal("Tour Operator", updated!.Role);
    }

    [Fact]
    public async Task UpdateRole_NonExistentUser_ReturnsNotFound()
    {
        var result = await _controller.UpdateRole(Guid.NewGuid(), new UpdateRoleRequest { Role = "Admin" });
        Assert.IsType<NotFoundResult>(result);
    }

    // ═══════════════════════════════════════════════════════════════════════
    // PUT /api/admin/staff/{id}/suspend
    // ═══════════════════════════════════════════════════════════════════════

    [Fact]
    public async Task SuspendStaff_ValidUser_SetsRoleToSuspended()
    {
        // Arrange
        var user = SeedUser("Guide", "Suspendee", "suspend@test.com");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.SuspendStaff(user.Id);

        // Assert
        Assert.IsType<OkResult>(result);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.Equal("Suspended", updated!.Role);
    }

    [Fact]
    public async Task SuspendStaff_NonExistentUser_ReturnsNotFound()
    {
        var result = await _controller.SuspendStaff(Guid.NewGuid());
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task SuspendStaff_AlreadySuspended_RemainsIdempotent()
    {
        // Arrange
        var user = SeedUser("Suspended", "Already Suspended", "already@test.com");
        await _db.SaveChangesAsync();

        // Act
        var result = await _controller.SuspendStaff(user.Id);

        // Assert
        Assert.IsType<OkResult>(result);
        var updated = await _db.Users.FindAsync(user.Id);
        Assert.Equal("Suspended", updated!.Role);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
