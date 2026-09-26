using Microsoft.EntityFrameworkCore;
using Travyle.Api.Models;

namespace Travyle.Api.Data;

public class TravyleDbContext : DbContext
{
    public TravyleDbContext(DbContextOptions<TravyleDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();

    // Booking vertical
    public DbSet<BookingSchedule> BookingSchedules => Set<BookingSchedule>();
    public DbSet<ScheduleAvailableDate> ScheduleAvailableDates => Set<ScheduleAvailableDate>();
    public DbSet<ScheduleTimeSlot> ScheduleTimeSlots => Set<ScheduleTimeSlot>();
    public DbSet<Booking> Bookings => Set<Booking>();
    public DbSet<DiscountRequest> DiscountRequests => Set<DiscountRequest>();
    public DbSet<PaymentEscrow> PaymentEscrows => Set<PaymentEscrow>();
    public DbSet<BookingAgentWorkflow> BookingAgentWorkflows => Set<BookingAgentWorkflow>();

    // Operations and travel planning
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<TravelerProfile> TravelerProfiles => Set<TravelerProfile>();
    public DbSet<PersonalizedItinerary> PersonalizedItineraries => Set<PersonalizedItinerary>();
    public DbSet<TourActivity> TourActivities => Set<TourActivity>();
    public DbSet<GuideAssignment> GuideAssignments => Set<GuideAssignment>();
    public DbSet<RouteLog> RouteLogs => Set<RouteLog>();
    public DbSet<DisruptionAlert> DisruptionAlerts => Set<DisruptionAlert>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // BookingSchedule
        modelBuilder.Entity<BookingSchedule>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PricePerPerson).HasColumnType("numeric(18,2)");
            entity.HasMany(e => e.AvailableDates)
                  .WithOne(d => d.BookingSchedule)
                  .HasForeignKey(d => d.BookingScheduleId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.TimeSlots)
                  .WithOne(t => t.BookingSchedule)
                  .HasForeignKey(t => t.BookingScheduleId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // Booking
        modelBuilder.Entity<Booking>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.BasePrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.ServiceFee).HasColumnType("numeric(18,2)");
            entity.Property(e => e.DiscountAmount).HasColumnType("numeric(18,2)");
            entity.Property(e => e.TotalAmount).HasColumnType("numeric(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PaymentStatus).HasConversion<string>();
            entity.Property(e => e.PaymentMethod).HasConversion<string>();
            entity.Property(e => e.ReceiptImageData).HasColumnType("text");
            entity.HasOne(e => e.Schedule)
                  .WithMany(s => s.Bookings)
                  .HasForeignKey(e => e.ScheduleId)
                  .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.PaymentEscrow)
                  .WithOne(pe => pe.Booking)
                  .HasForeignKey<PaymentEscrow>(pe => pe.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.DiscountRequest)
                  .WithOne(dr => dr.Booking)
                  .HasForeignKey<DiscountRequest>(dr => dr.BookingId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        // DiscountRequest
        modelBuilder.Entity<DiscountRequest>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.OriginalPrice).HasColumnType("numeric(18,2)");
            entity.Property(e => e.RequestedDiscountPercent).HasColumnType("numeric(5,2)");
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Ignore(e => e.CalculatedDiscountAmount);
        });

        // PaymentEscrow
        modelBuilder.Entity<PaymentEscrow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Amount).HasColumnType("numeric(18,2)");
            entity.Property(e => e.RefundedAmount).HasColumnType("numeric(18,2)");
            entity.Property(e => e.Status).HasConversion<string>();
        });

        modelBuilder.Entity<TravelerProfile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.User)
                  .WithOne(u => u.TravelerProfile)
                  .HasForeignKey<TravelerProfile>(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<PersonalizedItinerary>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Traveler)
                  .WithMany(u => u.PersonalizedItineraries)
                  .HasForeignKey(e => e.TravelerId)
                  .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<GuideAssignment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Guide)
                  .WithMany()
                  .HasForeignKey(e => e.GuideUserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<RouteLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasOne(e => e.Recorder)
                  .WithMany()
                  .HasForeignKey(e => e.RecordedBy)
                  .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<TourActivity>().HasKey(e => e.Id);
        modelBuilder.Entity<DisruptionAlert>().HasKey(e => e.Id);

        modelBuilder.Entity<BookingAgentWorkflow>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasConversion<string>();
            entity.Property(e => e.PlanJson).HasColumnType("text");
            entity.Property(e => e.CompletedStepsJson).HasColumnType("text");
            entity.Property(e => e.ToolResultsJson).HasColumnType("text");
            entity.Property(e => e.ValidationResultsJson).HasColumnType("text");
            entity.Property(e => e.ProposedBookingJson).HasColumnType("text");
        });
    }
}