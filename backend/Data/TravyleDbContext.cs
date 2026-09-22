using Microsoft.EntityFrameworkCore;
using Travyle.Api.Models;

namespace Travyle.Api.Data;

public class TravyleDbContext : DbContext
{
    public TravyleDbContext(DbContextOptions<TravyleDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<TravelerProfile> TravelerProfiles => Set<TravelerProfile>();
    public DbSet<Destination> Destinations => Set<Destination>();
    public DbSet<PersonalizedItinerary> PersonalizedItineraries => Set<PersonalizedItinerary>();
    // each vertical adds their own DbSets here as they build

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

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
    }
}