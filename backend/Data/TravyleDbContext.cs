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
    public DbSet<TravelerProfile> TravelerProfiles => Set<TravelerProfile>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<PersonalizedItinerary> PersonalizedItineraries => Set<PersonalizedItinerary>();

    // Component 4: Support & Customer Quality
    public DbSet<SupportTicket> SupportTickets => Set<SupportTicket>();
    public DbSet<Voucher> Vouchers => Set<Voucher>();
    public DbSet<CustomerReview> CustomerReviews => Set<CustomerReview>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

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
            
        // Configure 1-to-1 relationship between User and TravelerProfile
        modelBuilder.Entity<User>()
            .HasOne(u => u.TravelerProfile)
            .WithOne(tp => tp.User)
            .HasForeignKey<TravelerProfile>(tp => tp.UserId)
            .OnDelete(DeleteBehavior.Cascade);
            
        // Configure 1-to-Many relationship between User and PersonalizedItinerary
        modelBuilder.Entity<User>()
            .HasMany(u => u.PersonalizedItineraries)
            .WithOne(pi => pi.Traveler)
            .HasForeignKey(pi => pi.TravelerId)
            .OnDelete(DeleteBehavior.Cascade);

        // SupportTicket relationships
        modelBuilder.Entity<SupportTicket>()
            .HasOne(t => t.User)
            .WithMany()
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SupportTicket>()
            .HasMany(t => t.AuditLogs)
            .WithOne(a => a.SupportTicket)
            .HasForeignKey(a => a.SupportTicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<SupportTicket>()
            .HasMany(t => t.Vouchers)
            .WithOne(v => v.SupportTicket)
            .HasForeignKey(v => v.SupportTicketId)
            .OnDelete(DeleteBehavior.SetNull);

        // Voucher relationships
        modelBuilder.Entity<Voucher>()
            .HasIndex(v => v.Code)
            .IsUnique();

        modelBuilder.Entity<Voucher>()
            .HasOne(v => v.User)
            .WithMany()
            .HasForeignKey(v => v.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // CustomerReview relationships
        modelBuilder.Entity<CustomerReview>()
            .HasOne(r => r.User)
            .WithMany()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}