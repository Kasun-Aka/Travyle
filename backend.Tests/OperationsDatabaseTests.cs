using Microsoft.EntityFrameworkCore;
using Travyle.Api.Data;
using Travyle.Api.Models;
using Travyle.Api.Repositories;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Database integration tests for OperationsRepository.
/// Verifies that data access layer logic works as expected against an InMemory database.
/// </summary>
public class OperationsDatabaseTests : IDisposable
{
    private readonly TravyleDbContext _db;
    private readonly OperationsRepository _repository;

    public OperationsDatabaseTests()
    {
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
            
        _db = new TravyleDbContext(options);
        _repository = new OperationsRepository(_db);
    }

    [Fact]
    public async Task CreateTourActivityAsync_ValidActivity_SavesToDatabase()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var activity = new TourActivity
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = scheduleId,
            ActivityName = "Test Hike",
            ScheduledTime = DateTime.UtcNow,
            Location = "Mountain Base",
            Status = "Scheduled"
        };

        // Act
        var created = await _repository.CreateTourActivityAsync(activity);

        // Assert
        Assert.Equal("Test Hike", created.ActivityName);
        Assert.Single(await _db.TourActivities.ToListAsync());
    }

    [Fact]
    public async Task GetActivitiesByScheduleIdAsync_MultipleActivities_ReturnsOrderedByTime()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var now = DateTime.UtcNow;
        
        var act1 = new TourActivity { Id = Guid.NewGuid(), BookingScheduleId = scheduleId, ActivityName = "Later Activity", ScheduledTime = now.AddHours(2), Status = "Scheduled" };
        var act2 = new TourActivity { Id = Guid.NewGuid(), BookingScheduleId = scheduleId, ActivityName = "Earlier Activity", ScheduledTime = now.AddHours(1), Status = "Scheduled" };
        var act3 = new TourActivity { Id = Guid.NewGuid(), BookingScheduleId = Guid.NewGuid(), ActivityName = "Other Schedule", ScheduledTime = now, Status = "Scheduled" };
        
        _db.TourActivities.AddRange(act1, act2, act3);
        await _db.SaveChangesAsync();

        // Act
        var results = await _repository.GetActivitiesByScheduleIdAsync(scheduleId);

        // Assert
        Assert.Equal(2, results.Count);
        // Ordered by ScheduledTime: earlier should be first
        Assert.Equal("Earlier Activity", results[0].ActivityName);
        Assert.Equal("Later Activity", results[1].ActivityName);
    }

    [Fact]
    public async Task GetActiveAlertsAsync_ReturnsOnlyUnresolvedAlerts()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        
        var alert1 = new DisruptionAlert { Id = Guid.NewGuid(), BookingScheduleId = scheduleId, Type = "Weather", Severity = "High", TriggeredAt = DateTime.UtcNow.AddMinutes(-10), ResolvedAt = null }; // Active
        var alert2 = new DisruptionAlert { Id = Guid.NewGuid(), BookingScheduleId = scheduleId, Type = "Traffic", Severity = "Low", TriggeredAt = DateTime.UtcNow.AddMinutes(-30), ResolvedAt = DateTime.UtcNow }; // Resolved
        var alert3 = new DisruptionAlert { Id = Guid.NewGuid(), BookingScheduleId = Guid.NewGuid(), Type = "Weather", Severity = "Medium", TriggeredAt = DateTime.UtcNow, ResolvedAt = null }; // Active, different schedule
        
        _db.DisruptionAlerts.AddRange(alert1, alert2, alert3);
        await _db.SaveChangesAsync();

        // Act - Get for specific schedule
        var scheduleAlerts = await _repository.GetActiveAlertsAsync(scheduleId);
        
        // Act - Get for all schedules
        var allAlerts = await _repository.GetActiveAlertsAsync(null);

        // Assert
        Assert.Single(scheduleAlerts);
        Assert.Equal(alert1.Id, scheduleAlerts[0].Id);
        
        Assert.Equal(2, allAlerts.Count); // alert1 and alert3
    }
    
    [Fact]
    public async Task CreateRouteLogAsync_SavesLogSuccessfully()
    {
        // Arrange
        var log = new RouteLog
        {
            Id = Guid.NewGuid(),
            BookingScheduleId = Guid.NewGuid(),
            RecordedBy = Guid.NewGuid(),
            Latitude = 7.2906m,
            Longitude = 80.6337m,
            Timestamp = DateTime.UtcNow
        };

        // Act
        var result = await _repository.CreateRouteLogAsync(log);

        // Assert
        Assert.NotNull(result);
        var inDb = await _db.RouteLogs.FindAsync(log.Id);
        Assert.NotNull(inDb);
        Assert.Equal(7.2906m, inDb!.Latitude);
    }

    public void Dispose()
    {
        _db.Database.EnsureDeleted();
        _db.Dispose();
    }
}
