using Travyle.Api.Models;

namespace Travyle.Api.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(TravyleDbContext db)
    {
        if (db.Users.Any()) return;

        var traveler = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            FirebaseUid = "traveler-demo-uid",
            Email = "sarah.traveler@example.com",
            FullName = "Sarah Jenkins",
            Role = "Traveler",
            CreatedAt = DateTime.UtcNow.AddDays(-10)
        };

        var admin = new User
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            FirebaseUid = "admin-demo-uid",
            Email = "admin@travyle.io",
            FullName = "Elena Rostova",
            Role = "Administrator",
            CreatedAt = DateTime.UtcNow.AddDays(-30)
        };

        await db.Users.AddRangeAsync(traveler, admin);

        var tourId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        // Ticket 1: Tour delay issue that triggered AI $50 voucher draft
        var ticket1 = new SupportTicket
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            UserId = traveler.Id,
            TourId = tourId,
            Title = "3-Hour Unannounced Tour Delay in Alpine Excursion",
            Description = "Our tour bus was delayed by over 3 hours with no prior warning or hydration provided. It ruined the sunset overlook schedule and we missed the mountain cable car.",
            Category = "TourDelay",
            Priority = TicketPriority.High,
            Status = TicketStatus.Pending_Admin_Voucher_Approval,
            AttachmentUrl = "https://images.unsplash.com/photo-1544620347-c4fd4a3d5957?w=600",
            SentimentScore = -0.78,
            SeverityTier = "Tier_2_High",
            AiReasoning = "Sentiment: -0.78 (Severe Disappointment) | Tier: Tier_2_High | Recommendation: $50.00 Goodwill Discount Voucher drafted for customer retention.",
            CreatedAt = DateTime.UtcNow.AddHours(-4),
            UpdatedAt = DateTime.UtcNow.AddHours(-4)
        };

        var audit1 = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket1.Id,
            Action = "TICKET_CREATED",
            ActorRole = "Traveler",
            ActorId = traveler.Id.ToString(),
            Details = "Ticket submitted via mobile app with photo attachment.",
            Timestamp = DateTime.UtcNow.AddHours(-4)
        };

        var audit2 = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket1.Id,
            Action = "AI_SENTIMENT_ANALYZED",
            ActorRole = "Support_AI_Agent",
            ActorId = "agent-sentiment-v1",
            Details = "Evaluated ticket content. Extracted sentiment score -0.78 (Negative/Frustrated).",
            Timestamp = DateTime.UtcNow.AddHours(-4).AddSeconds(2)
        };

        var audit3 = new AuditLog
        {
            Id = Guid.NewGuid(),
            SupportTicketId = ticket1.Id,
            Action = "AI_VOUCHER_DRAFTED",
            ActorRole = "Support_AI_Agent",
            ActorId = "agent-policy-v1",
            Details = "Drafted $50.00 Goodwill Voucher [TRAV-GW-7842-0921]. Paused for Administrator Sign-Off.",
            Timestamp = DateTime.UtcNow.AddHours(-4).AddSeconds(5)
        };

        var voucher1 = new Voucher
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Code = "TRAV-GW-7842-0921",
            UserId = traveler.Id,
            SupportTicketId = ticket1.Id,
            Amount = 50.00m,
            Reason = "Goodwill compensation for Alpine tour delay",
            Status = VoucherStatus.Draft,
            ExpiresAt = DateTime.UtcNow.AddDays(90),
            CreatedAt = DateTime.UtcNow.AddHours(-4),
            UpdatedAt = DateTime.UtcNow.AddHours(-4)
        };

        // Ticket 2: General inquiry (Resolved)
        var ticket2 = new SupportTicket
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            UserId = traveler.Id,
            Title = "Luggage storage inquiry for Ella train transfer",
            Description = "Does the private shuttle allow 2 oversized surf bags per passenger?",
            Category = "General",
            Priority = TicketPriority.Low,
            Status = TicketStatus.Resolved,
            SentimentScore = 0.20,
            SeverityTier = "Tier_1_Low",
            AiReasoning = "Sentiment: 0.20 | Tier: Tier_1_Low | Standard logistical inquiry.",
            ResolutionSummary = "Customer confirmed that standard oversized baggage allowance includes up to 2 surf bags with advance driver notice.",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1)
        };

        var review1 = new CustomerReview
        {
            Id = Guid.NewGuid(),
            UserId = traveler.Id,
            TourId = tourId,
            Rating = 5,
            Comment = "Apart from the bus delay which support handled wonderfully, the mountain vistas and guide knowledge were exceptional!",
            IsVerified = true,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };

        await db.SupportTickets.AddRangeAsync(ticket1, ticket2);
        await db.AuditLogs.AddRangeAsync(audit1, audit2, audit3);
        await db.Vouchers.AddAsync(voucher1);
        await db.CustomerReviews.AddAsync(review1);

        await db.SaveChangesAsync();
    }
}
