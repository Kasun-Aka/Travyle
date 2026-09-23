using Microsoft.EntityFrameworkCore;
using Travyle.Api.Models;

namespace Travyle.Api.Data;

public class TravyleDbContext : DbContext
{
    public TravyleDbContext(DbContextOptions<TravyleDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    // Operations – Tour Guide vertical
    public DbSet<TourActivity> TourActivities => Set<TourActivity>();
    public DbSet<GuideAssignment> GuideAssignments => Set<GuideAssignment>();
    public DbSet<RouteLog> RouteLogs => Set<RouteLog>();
    public DbSet<DisruptionAlert> DisruptionAlerts => Set<DisruptionAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // GuideAssignment → User (guide)
        modelBuilder.Entity<GuideAssignment>()
            .HasOne(ga => ga.Guide)
            .WithMany()
            .HasForeignKey(ga => ga.GuideUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // RouteLog → User (recorder)
        modelBuilder.Entity<RouteLog>()
            .HasOne(r => r.Recorder)
            .WithMany()
            .HasForeignKey(r => r.RecordedBy)
            .OnDelete(DeleteBehavior.Restrict);

        // Index for fast GPS-ping queries per tour
        modelBuilder.Entity<RouteLog>()
            .HasIndex(r => new { r.BookingScheduleId, r.Timestamp });

        // Index for active alerts lookup
        modelBuilder.Entity<DisruptionAlert>()
            .HasIndex(d => new { d.BookingScheduleId, d.ResolvedAt });
    }
}