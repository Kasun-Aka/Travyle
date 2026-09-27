using Microsoft.EntityFrameworkCore;
using Travyle.Api.Models;

namespace Travyle.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(TravyleDbContext db)
    {
        // Ensure dev traveler always exists (upsert by ID)
        var devTravelerId = Guid.Parse("00000000-0000-0000-0000-000000000001");
        if (!await db.Users.AnyAsync(u => u.Id == devTravelerId))
        {
            db.Users.Add(new User
            {
                Id = devTravelerId,
                FirebaseUid = "dev-traveler-uid-001",
                Email = "traveler@travyle.com",
                FullName = "Nithu Traveler",
                Role = "Traveler",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        if (!await db.Users.AnyAsync(u => u.Role == "Admin"))
        {
            db.Users.Add(new User
            {
                Id = Guid.Parse("00000000-0000-0000-0000-000000000099"),
                FirebaseUid = "dev-admin-uid-099",
                Email = "admin@travyle.com",
                FullName = "Travyle Admin",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow
            });
            await db.SaveChangesAsync();
        }

        // 2. Seed booking schedules if none exist
        if (!await db.BookingSchedules.AnyAsync())
        {
            var now = DateTime.UtcNow.Date;

            // Generate rolling available dates for the next 30 days
            List<ScheduleAvailableDate> GenerateDates(int[] dayOffsets)
            {
                return dayOffsets.Select(offset => new ScheduleAvailableDate
                {
                    Id = Guid.NewGuid(),
                    Date = DateTime.SpecifyKind(now.AddDays(offset), DateTimeKind.Utc)
                }).ToList();
            }

            List<ScheduleTimeSlot> GenerateSlots(string[] slots)
            {
                return slots.Select(slot => new ScheduleTimeSlot
                {
                    Id = Guid.NewGuid(),
                    SlotLabel = slot
                }).ToList();
            }

            var schedules = new List<BookingSchedule>
            {
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000001"),
                    DestinationId = Guid.Parse("00000000-0000-0000-0002-000000000001"),
                    DestinationTitle = "Ella Rock & Nine Arch Bridge Trek",
                    Location = "Ella, Badulla District",
                    GuideName = "Kasun Bandara",
                    PricePerPerson = 4500.00m,
                    MaxCapacityPerSlot = 8,
                    Rating = 4.9,
                    ReviewsCount = 142,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvailableDates = GenerateDates(Enumerable.Range(1, 30).ToArray()),
                    TimeSlots = GenerateSlots(new[] { "06:30 AM", "09:00 AM", "02:00 PM", "04:30 PM" })
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000002"),
                    DestinationId = Guid.Parse("00000000-0000-0000-0002-000000000002"),
                    DestinationTitle = "Sigiriya Ancient Rock Fortress Sunrise Tour",
                    Location = "Sigiriya, Matale District",
                    GuideName = "Anura Senanayake",
                    PricePerPerson = 6000.00m,
                    MaxCapacityPerSlot = 10,
                    Rating = 4.8,
                    ReviewsCount = 98,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvailableDates = GenerateDates(Enumerable.Range(1, 30).ToArray()),
                    TimeSlots = GenerateSlots(new[] { "05:30 AM", "08:00 AM", "03:30 PM" })
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000003"),
                    DestinationId = Guid.Parse("00000000-0000-0000-0002-000000000003"),
                    DestinationTitle = "Mirissa Blue Whale Watching Expedition",
                    Location = "Mirissa Harbour, Southern Province",
                    GuideName = "Chaminda Silva",
                    PricePerPerson = 8500.00m,
                    MaxCapacityPerSlot = 12,
                    Rating = 4.7,
                    ReviewsCount = 84,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvailableDates = GenerateDates(Enumerable.Range(1, 30).ToArray()),
                    TimeSlots = GenerateSlots(new[] { "06:00 AM", "07:30 AM" })
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000004"),
                    DestinationId = Guid.Parse("00000000-0000-0000-0002-000000000001"),
                    DestinationTitle = "Little Adam's Peak & Tea Factory Experience",
                    Location = "Ella, Badulla District",
                    GuideName = "Niroshan Perera",
                    PricePerPerson = 3800.00m,
                    MaxCapacityPerSlot = 6,
                    Rating = 4.9,
                    ReviewsCount = 110,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvailableDates = GenerateDates(Enumerable.Range(1, 30).ToArray()),
                    TimeSlots = GenerateSlots(new[] { "08:30 AM", "11:00 AM", "03:00 PM" })
                },
                new()
                {
                    Id = Guid.Parse("00000000-0000-0000-0001-000000000005"),
                    DestinationId = Guid.Parse("00000000-0000-0000-0002-000000000002"),
                    DestinationTitle = "Pidurangala Rock Sunset & Village Cycle Tour",
                    Location = "Sigiriya, Matale District",
                    GuideName = "Sunil Wickramasinghe",
                    PricePerPerson = 4200.00m,
                    MaxCapacityPerSlot = 8,
                    Rating = 4.6,
                    ReviewsCount = 52,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    AvailableDates = GenerateDates(Enumerable.Range(1, 30).ToArray()),
                    TimeSlots = GenerateSlots(new[] { "07:00 AM", "04:00 PM" })
                }
            };

            db.BookingSchedules.AddRange(schedules);
            await db.SaveChangesAsync();
        }
    }
}
