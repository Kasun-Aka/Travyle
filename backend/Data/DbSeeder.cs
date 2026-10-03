using Microsoft.EntityFrameworkCore;
using Travyle.Api.Models;

namespace Travyle.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(TravyleDbContext db)
    {
        // ═══════════════════════════════════════════════════════════
        // 0. ENSURE FK CONSTRAINTS — clean orphans then add missing FKs
        //    (avoids EF migration drift issues with timestamp types)
        // ═══════════════════════════════════════════════════════════

        // First, delete any orphaned rows that reference non-existent BookingSchedules
        var cleanupStatements = new[]
        {
            @"DELETE FROM ""DisruptionAlerts"" WHERE ""BookingScheduleId"" NOT IN (SELECT ""Id"" FROM ""BookingSchedules"");",
            @"DELETE FROM ""TourActivities"" WHERE ""BookingScheduleId"" NOT IN (SELECT ""Id"" FROM ""BookingSchedules"");",
            @"DELETE FROM ""RouteLogs"" WHERE ""BookingScheduleId"" NOT IN (SELECT ""Id"" FROM ""BookingSchedules"");",
            @"DELETE FROM ""GuideAssignments"" WHERE ""BookingScheduleId"" NOT IN (SELECT ""Id"" FROM ""BookingSchedules"");"
        };

        foreach (var sql in cleanupStatements)
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }
        var fkStatements = new[]
        {
            @"DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_DisruptionAlerts_BookingSchedules_BookingScheduleId') THEN
                    ALTER TABLE ""DisruptionAlerts"" ADD CONSTRAINT ""FK_DisruptionAlerts_BookingSchedules_BookingScheduleId"" FOREIGN KEY (""BookingScheduleId"") REFERENCES ""BookingSchedules""(""Id"") ON DELETE CASCADE;
                END IF;
              END $$;",
            @"DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_TourActivities_BookingSchedules_BookingScheduleId') THEN
                    ALTER TABLE ""TourActivities"" ADD CONSTRAINT ""FK_TourActivities_BookingSchedules_BookingScheduleId"" FOREIGN KEY (""BookingScheduleId"") REFERENCES ""BookingSchedules""(""Id"") ON DELETE CASCADE;
                END IF;
              END $$;",
            @"DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_RouteLogs_BookingSchedules_BookingScheduleId') THEN
                    ALTER TABLE ""RouteLogs"" ADD CONSTRAINT ""FK_RouteLogs_BookingSchedules_BookingScheduleId"" FOREIGN KEY (""BookingScheduleId"") REFERENCES ""BookingSchedules""(""Id"") ON DELETE CASCADE;
                END IF;
              END $$;",
            @"DO $$ BEGIN
                IF NOT EXISTS (SELECT 1 FROM information_schema.table_constraints WHERE constraint_name = 'FK_GuideAssignments_BookingSchedules_BookingScheduleId') THEN
                    ALTER TABLE ""GuideAssignments"" ADD CONSTRAINT ""FK_GuideAssignments_BookingSchedules_BookingScheduleId"" FOREIGN KEY (""BookingScheduleId"") REFERENCES ""BookingSchedules""(""Id"") ON DELETE CASCADE;
                END IF;
              END $$;"
        };

        foreach (var sql in fkStatements)
        {
            await db.Database.ExecuteSqlRawAsync(sql);
        }

        // ═══════════════════════════════════════════════════════════
        // 1. USERS — dev traveler, admin, and 5 guides
        // ═══════════════════════════════════════════════════════════
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

        // Guide users — IDs match the order of BookingSchedules
        var guideSeeds = new[]
        {
            new { Id = Guid.Parse("00000000-0000-0000-0000-100000000001"), Uid = "guide-uid-001", Email = "kasun@travyle.com",    Name = "Kasun Bandara" },
            new { Id = Guid.Parse("00000000-0000-0000-0000-100000000002"), Uid = "guide-uid-002", Email = "anura@travyle.com",    Name = "Anura Senanayake" },
            new { Id = Guid.Parse("00000000-0000-0000-0000-100000000003"), Uid = "guide-uid-003", Email = "chaminda@travyle.com", Name = "Chaminda Silva" },
            new { Id = Guid.Parse("00000000-0000-0000-0000-100000000004"), Uid = "guide-uid-004", Email = "niroshan@travyle.com", Name = "Niroshan Perera" },
            new { Id = Guid.Parse("00000000-0000-0000-0000-100000000005"), Uid = "guide-uid-005", Email = "sunil@travyle.com",    Name = "Sunil Wickramasinghe" },
        };

        foreach (var g in guideSeeds)
        {
            if (!await db.Users.AnyAsync(u => u.Id == g.Id))
            {
                db.Users.Add(new User
                {
                    Id = g.Id,
                    FirebaseUid = g.Uid,
                    Email = g.Email,
                    FullName = g.Name,
                    Role = "Local Guide",
                    CreatedAt = DateTime.UtcNow
                });
            }
        }
        await db.SaveChangesAsync();

        // ═══════════════════════════════════════════════════════════
        // 2. BOOKING SCHEDULES (only if none exist — keep existing logic)
        // ═══════════════════════════════════════════════════════════
        if (!await db.BookingSchedules.AnyAsync())
        {
            var now = DateTime.UtcNow.Date;

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

        // ═══════════════════════════════════════════════════════════
        // 3. GUIDE ASSIGNMENTS — link each guide to their schedule
        // ═══════════════════════════════════════════════════════════
        if (!await db.GuideAssignments.AnyAsync())
        {
            var assignments = new List<GuideAssignment>
            {
                new() { Id = Guid.Parse("00000000-0000-0000-0003-000000000001"), BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"), GuideUserId = Guid.Parse("00000000-0000-0000-0000-100000000001"), Status = "Active",    AssignedAt = DateTime.UtcNow.AddDays(-2) },
                new() { Id = Guid.Parse("00000000-0000-0000-0003-000000000002"), BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000002"), GuideUserId = Guid.Parse("00000000-0000-0000-0000-100000000002"), Status = "Active",    AssignedAt = DateTime.UtcNow.AddDays(-1) },
                new() { Id = Guid.Parse("00000000-0000-0000-0003-000000000003"), BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003"), GuideUserId = Guid.Parse("00000000-0000-0000-0000-100000000003"), Status = "Assigned",  AssignedAt = DateTime.UtcNow },
                new() { Id = Guid.Parse("00000000-0000-0000-0003-000000000004"), BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000004"), GuideUserId = Guid.Parse("00000000-0000-0000-0000-100000000004"), Status = "Active",    AssignedAt = DateTime.UtcNow.AddDays(-3) },
                new() { Id = Guid.Parse("00000000-0000-0000-0003-000000000005"), BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000005"), GuideUserId = Guid.Parse("00000000-0000-0000-0000-100000000005"), Status = "Assigned",  AssignedAt = DateTime.UtcNow },
            };
            db.GuideAssignments.AddRange(assignments);
            await db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 4. TOUR ACTIVITIES — realistic stops per schedule
        // ═══════════════════════════════════════════════════════════
        if (!await db.TourActivities.AnyAsync())
        {
            var today = DateTime.UtcNow.Date;
            var activities = new List<TourActivity>
            {
                // Ella Rock & Nine Arch Bridge (Schedule 1)
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"), ActivityName = "Ella Rock Summit Hike",        ScheduledTime = today.AddHours(6.5),  Location = "Ella Rock Trailhead",       Status = "Completed" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"), ActivityName = "Nine Arch Bridge Photo Stop",   ScheduledTime = today.AddHours(9),    Location = "Demodara Nine Arch Bridge", Status = "InProgress" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"), ActivityName = "Tea Plantation Visit",          ScheduledTime = today.AddHours(11),   Location = "Lipton's Seat Area",        Status = "Scheduled" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"), ActivityName = "Local Lunch at Ella Village",   ScheduledTime = today.AddHours(13),   Location = "Ella Town Center",          Status = "Scheduled" },

                // Sigiriya (Schedule 2)
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000002"), ActivityName = "Sigiriya Lion Rock Climb",      ScheduledTime = today.AddHours(5.5),  Location = "Sigiriya Entrance",         Status = "Completed" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000002"), ActivityName = "Mirror Wall & Frescoes Tour",   ScheduledTime = today.AddHours(7),    Location = "Sigiriya Mid-Level",        Status = "InProgress" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000002"), ActivityName = "Royal Gardens Exploration",     ScheduledTime = today.AddHours(9),    Location = "Sigiriya Water Gardens",    Status = "Scheduled" },

                // Mirissa Whale Watch (Schedule 3)
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003"), ActivityName = "Harbour Departure & Safety Brief", ScheduledTime = today.AddHours(6),  Location = "Mirissa Harbour",           Status = "Completed" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003"), ActivityName = "Deep Sea Whale Spotting",       ScheduledTime = today.AddHours(7.5),  Location = "Open Ocean, 8km Offshore",  Status = "InProgress" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003"), ActivityName = "Return & Beach Stop",           ScheduledTime = today.AddHours(10),   Location = "Mirissa Beach",             Status = "Scheduled" },

                // Little Adam's Peak (Schedule 4)
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000004"), ActivityName = "Little Adam's Peak Sunrise Walk", ScheduledTime = today.AddHours(8.5), Location = "98 Acres Resort Trailhead", Status = "InProgress" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000004"), ActivityName = "Ceylon Tea Factory Tour",       ScheduledTime = today.AddHours(11),   Location = "Uva Halpewatte Factory",    Status = "Scheduled" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000004"), ActivityName = "Ravana Falls Quick Stop",       ScheduledTime = today.AddHours(14),   Location = "Ravana Falls Viewpoint",    Status = "Scheduled" },

                // Pidurangala (Schedule 5)
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000005"), ActivityName = "Village Cycle Tour",            ScheduledTime = today.AddHours(7),    Location = "Habarana Village",           Status = "Completed" },
                new() { BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000005"), ActivityName = "Pidurangala Rock Ascent",       ScheduledTime = today.AddHours(16),   Location = "Pidurangala Temple Base",    Status = "Scheduled" },
            };
            db.TourActivities.AddRange(activities);
            await db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 5. ROUTE LOGS — GPS pings simulating guide movement
        // ═══════════════════════════════════════════════════════════
        if (!await db.RouteLogs.AnyAsync())
        {
            var baseTime = DateTime.UtcNow.AddHours(-3);

            // Ella tour (Guide: Kasun Bandara) — coords around Ella (6.8667, 81.0466)
            var ellaGuideId = Guid.Parse("00000000-0000-0000-0000-100000000001");
            var ellaScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001");

            // Sigiriya tour (Guide: Anura) — coords around Sigiriya (7.9570, 80.7603)
            var sigiriyaGuideId = Guid.Parse("00000000-0000-0000-0000-100000000002");
            var sigiriyaScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000002");

            // Mirissa tour (Guide: Chaminda) — coords around Mirissa (5.9485, 80.4528)
            var mirissaGuideId = Guid.Parse("00000000-0000-0000-0000-100000000003");
            var mirissaScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003");

            var routeLogs = new List<RouteLog>
            {
                // Ella route pings (every ~15 min)
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8667m, Longitude = 81.0466m, Timestamp = baseTime },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8680m, Longitude = 81.0470m, Timestamp = baseTime.AddMinutes(15) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8705m, Longitude = 81.0490m, Timestamp = baseTime.AddMinutes(30) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8730m, Longitude = 81.0510m, Timestamp = baseTime.AddMinutes(45) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8755m, Longitude = 81.0525m, Timestamp = baseTime.AddMinutes(60) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8770m, Longitude = 81.0545m, Timestamp = baseTime.AddMinutes(75) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8790m, Longitude = 81.0560m, Timestamp = baseTime.AddMinutes(90) },
                new() { BookingScheduleId = ellaScheduleId, RecordedBy = ellaGuideId, Latitude = 6.8811m, Longitude = 81.0580m, Timestamp = baseTime.AddMinutes(105) },

                // Sigiriya route pings
                new() { BookingScheduleId = sigiriyaScheduleId, RecordedBy = sigiriyaGuideId, Latitude = 7.9570m, Longitude = 80.7603m, Timestamp = baseTime },
                new() { BookingScheduleId = sigiriyaScheduleId, RecordedBy = sigiriyaGuideId, Latitude = 7.9575m, Longitude = 80.7610m, Timestamp = baseTime.AddMinutes(20) },
                new() { BookingScheduleId = sigiriyaScheduleId, RecordedBy = sigiriyaGuideId, Latitude = 7.9582m, Longitude = 80.7625m, Timestamp = baseTime.AddMinutes(40) },
                new() { BookingScheduleId = sigiriyaScheduleId, RecordedBy = sigiriyaGuideId, Latitude = 7.9590m, Longitude = 80.7640m, Timestamp = baseTime.AddMinutes(60) },
                new() { BookingScheduleId = sigiriyaScheduleId, RecordedBy = sigiriyaGuideId, Latitude = 7.9600m, Longitude = 80.7655m, Timestamp = baseTime.AddMinutes(80) },

                // Mirissa route pings
                new() { BookingScheduleId = mirissaScheduleId, RecordedBy = mirissaGuideId, Latitude = 5.9485m, Longitude = 80.4528m, Timestamp = baseTime },
                new() { BookingScheduleId = mirissaScheduleId, RecordedBy = mirissaGuideId, Latitude = 5.9400m, Longitude = 80.4450m, Timestamp = baseTime.AddMinutes(25) },
                new() { BookingScheduleId = mirissaScheduleId, RecordedBy = mirissaGuideId, Latitude = 5.9300m, Longitude = 80.4350m, Timestamp = baseTime.AddMinutes(50) },
                new() { BookingScheduleId = mirissaScheduleId, RecordedBy = mirissaGuideId, Latitude = 5.9200m, Longitude = 80.4250m, Timestamp = baseTime.AddMinutes(75) },
            };
            db.RouteLogs.AddRange(routeLogs);
            await db.SaveChangesAsync();
        }

        // ═══════════════════════════════════════════════════════════
        // 6. DISRUPTION ALERTS — one sample alert for testing
        // ═══════════════════════════════════════════════════════════
        if (!await db.DisruptionAlerts.AnyAsync())
        {
            db.DisruptionAlerts.Add(new DisruptionAlert
            {
                Id = Guid.Parse("00000000-0000-0000-0004-000000000001"),
                BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000001"),
                Type = "Weather",
                Severity = "Medium",
                Description = "Heavy rainfall expected in Ella region between 2:00 PM - 5:00 PM. Consider rescheduling afternoon activities.",
                TriggeredAt = DateTime.UtcNow.AddHours(-1),
                ResolvedAt = null
            });
            db.DisruptionAlerts.Add(new DisruptionAlert
            {
                Id = Guid.Parse("00000000-0000-0000-0004-000000000002"),
                BookingScheduleId = Guid.Parse("00000000-0000-0000-0001-000000000003"),
                Type = "Traffic",
                Severity = "Low",
                Description = "Minor road congestion near Mirissa Harbour. Boats departing on schedule.",
                TriggeredAt = DateTime.UtcNow.AddHours(-2),
                ResolvedAt = DateTime.UtcNow.AddMinutes(-30)
            });
            await db.SaveChangesAsync();
        }
    }
}
