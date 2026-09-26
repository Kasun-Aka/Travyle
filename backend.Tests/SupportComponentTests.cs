using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services;
using Xunit;

namespace Travyle.Api.Tests;

public class SupportComponentTests
{
    private TravyleDbContext CreateInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(databaseName: $"Travyle_Test_{Guid.NewGuid()}")
            .Options;

        var context = new TravyleDbContext(options);
        context.Database.EnsureCreated();
        return context;
    }

    private IConfiguration CreateTestConfiguration()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            { "SendGrid:ApiKey", "test-key" },
            { "SendGrid:SenderEmail", "support@travyle.com" },
            { "Twilio:AccountSid", "test-sid" }
        };

        return new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Fact]
    public async Task TicketCreation_ShouldTriggerAiTriage_AndDraftGoodwillVoucher()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var createDto = new CreateTicketDto
        {
            Title = "Severe 4-Hour Tour Bus Delay",
            Description = "Our tour bus was delayed 4 hours without AC. Missed the morning excursion completely.",
            Category = "TourDelay",
            Priority = TicketPriority.High
        };

        // Act
        var result = await supportService.CreateTicketAsync(createDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(TicketPriority.High.ToString(), result.Priority);
        Assert.Equal("Pending_Admin_Voucher_Approval", result.Status);
        Assert.True(result.SentimentScore < 0, "Sentiment score should be negative for severe delay complaint.");
        Assert.Equal("Tier_2_High", result.SeverityTier);

        // Verify Audit Logs generated
        Assert.NotEmpty(result.AuditLogs);
        Assert.Contains(result.AuditLogs, a => a.Action == "TICKET_CREATED");
        Assert.Contains(result.AuditLogs, a => a.Action == "AI_SENTIMENT_ANALYZED");
        Assert.Contains(result.AuditLogs, a => a.Action == "AI_VOUCHER_DRAFTED");

        // Verify Draft Voucher was persisted in PostgreSQL / DB
        var vouchers = await db.Vouchers.Where(v => v.SupportTicketId == result.Id).ToListAsync();
        Assert.Single(vouchers);
        Assert.Equal(VoucherStatus.Draft, vouchers[0].Status);
        Assert.Equal(50.00m, vouchers[0].Amount);
    }

    [Fact]
    public async Task NonCrud_AutoResolveClaim_ShouldCalculateSentimentAndSeverity()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Title = "Emergency: Unsafe boat ride during storm",
            Description = "The captain went out in dangerous waves despite high wind warnings. We were terrified and unsafe.",
            Category = "Safety",
            Priority = TicketPriority.Critical,
            Status = TicketStatus.Pending_AI_Triage
        };
        await db.SupportTickets.AddAsync(ticket);
        await db.SaveChangesAsync();

        // Act
        var result = await supportService.AutoResolveClaimAsync(ticket.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(ticket.Id, result.TicketId);
        Assert.Equal("Tier_3_Critical", result.SeverityTier);
        Assert.True(result.VoucherDrafted);
        Assert.Equal(100.00m, result.VoucherAmount);
        Assert.NotEmpty(result.GeneratedAuditLogs);
    }

    [Fact]
    public async Task ApproveVoucher_ShouldActivateVoucher_ResolveTicket_AndTriggerNotification()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "traveler.test@example.com",
            FullName = "John Doe",
            Role = "Traveler"
        };
        await db.Users.AddAsync(user);

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Tour Delay Dispute",
            Description = "Delayed schedule.",
            Status = TicketStatus.Pending_Admin_Voucher_Approval
        };
        await db.SupportTickets.AddAsync(ticket);

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            Code = "TRAV-GW-9999",
            UserId = user.Id,
            SupportTicketId = ticket.Id,
            Amount = 50.00m,
            Status = VoucherStatus.Draft,
            Reason = "Goodwill compensation"
        };
        await db.Vouchers.AddAsync(voucher);
        await db.SaveChangesAsync();

        var adminId = Guid.NewGuid();
        var approveDto = new ApproveVoucherDto
        {
            AdminId = adminId,
            AdjustedAmount = 50.00m,
            Notes = "Approved by Support Lead.",
            SendNotification = true
        };

        // Act
        var result = await supportService.ApproveVoucherAsync(voucher.Id, approveDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VoucherStatus.Active.ToString(), result.Status);

        // Verify Ticket is now Resolved
        var updatedTicket = await db.SupportTickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(TicketStatus.Resolved, updatedTicket.Status);
        Assert.Contains("Approved $50.00 Goodwill Voucher", updatedTicket.ResolutionSummary);

        // Verify Audit Log
        var audits = await db.AuditLogs.Where(a => a.SupportTicketId == ticket.Id).ToListAsync();
        Assert.Contains(audits, a => a.Action == "VOUCHER_APPROVED_AND_ACTIVATED");
    }

    [Fact]
    public async Task CustomerReviews_SubmissionAndRetrieval_ShouldWorkCorrectly()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var tourId = Guid.NewGuid();
        var createDto = new CreateReviewDto
        {
            TourId = tourId,
            Rating = 5,
            Comment = "Breathtaking views and wonderfully organized itinerary!"
        };

        // Act
        var created = await supportService.CreateReviewAsync(createDto);
        var reviews = await supportService.GetReviewsByTourIdAsync(tourId);

        // Assert
        Assert.NotNull(created);
        Assert.Equal(5, created.Rating);
        Assert.True(created.IsVerified);
        Assert.Single(reviews);
        Assert.Equal("Breathtaking views and wonderfully organized itinerary!", reviews[0].Comment);
    }

    [Fact]
    public async Task RejectVoucher_ShouldRevokeVoucher_AndUpdateTicketStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var user = new User { Id = Guid.NewGuid(), Email = "testuser@travyle.com", FullName = "Test Traveler" };
        await db.Users.AddAsync(user);

        var ticket = new SupportTicket
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Title = "Minor Disruption Claim",
            Description = "Minor delay.",
            Status = TicketStatus.Pending_Admin_Voucher_Approval
        };
        await db.SupportTickets.AddAsync(ticket);

        var voucher = new Voucher
        {
            Id = Guid.NewGuid(),
            Code = "TRAV-GW-7777",
            UserId = ticket.UserId,
            SupportTicketId = ticket.Id,
            SupportTicket = ticket,
            Amount = 50.00m,
            Status = VoucherStatus.Draft,
            Reason = "Goodwill draft"
        };
        await db.Vouchers.AddAsync(voucher);
        await db.SaveChangesAsync();

        var rejectDto = new RejectVoucherDto
        {
            AdminId = Guid.NewGuid(),
            Reason = "Claim does not meet goodwill threshold."
        };

        // Act
        var result = await supportService.RejectVoucherAsync(voucher.Id, rejectDto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(VoucherStatus.Revoked.ToString(), result.Status);

        var updatedTicket = await db.SupportTickets.FindAsync(ticket.Id);
        Assert.NotNull(updatedTicket);
        Assert.Equal(TicketStatus.Rejected, updatedTicket.Status);
        Assert.Contains("Declined Goodwill Voucher", updatedTicket.ResolutionSummary);
    }

    [Fact]
    public async Task ToggleReviewVerification_ShouldUpdateVerificationStatus()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var tourId = Guid.NewGuid();
        var review = await supportService.CreateReviewAsync(new CreateReviewDto
        {
            TourId = tourId,
            Rating = 4,
            Comment = "Good tour experience."
        });

        // Act
        var updated = await supportService.ToggleReviewVerificationAsync(review.Id, false);

        // Assert
        Assert.NotNull(updated);
        Assert.False(updated.IsVerified);
    }

    [Fact]
    public async Task UpdateTicket_ShouldEditDetails_WhenNotClosed()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var created = await supportService.CreateTicketAsync(new CreateTicketDto
        {
            Title = "Initial Complaint Title",
            Description = "Initial description",
            Category = "General",
            Priority = TicketPriority.Low
        });

        // Act
        var edited = await supportService.UpdateTicketAsync(created.Id, new UpdateTicketDto
        {
            Title = "Updated Bus Delay Complaint Title",
            Description = "Updated detailed description of incident.",
            Category = "TourDelay",
            Priority = TicketPriority.High
        });

        // Assert
        Assert.NotNull(edited);
        Assert.Equal("Updated Bus Delay Complaint Title", edited.Title);
        Assert.Equal("TourDelay", edited.Category);
        Assert.Equal("High", edited.Priority);
    }

    [Fact]
    public async Task SoftDelete_ShouldExcludeDeletedTicketsAndVouchers()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var created = await supportService.CreateTicketAsync(new CreateTicketDto
        {
            Title = "Ticket to be deleted",
            Description = "Soft delete test",
            Category = "General"
        });

        // Act
        var deleteResult = await supportService.DeleteTicketAsync(created.Id);
        var tickets = await supportService.GetTicketsAsync(new TicketListQueryDto());

        // Assert
        Assert.True(deleteResult);
        Assert.DoesNotContain(tickets.Items, t => t.Id == created.Id);
    }

    [Fact]
    public async Task DeleteReview_ShouldRemoveReviewFromDatabase()
    {
        // Arrange
        using var db = CreateInMemoryDbContext();
        var config = CreateTestConfiguration();
        var notificationService = new NotificationService(NullLogger<NotificationService>.Instance, config);
        var aiAgentService = new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance);
        var supportService = new SupportService(db, aiAgentService, notificationService, NullLogger<SupportService>.Instance);

        var tourId = Guid.NewGuid();
        var review = await supportService.CreateReviewAsync(new CreateReviewDto
        {
            TourId = tourId,
            Rating = 3,
            Comment = "Review to be deleted"
        });

        // Act
        var deleteResult = await supportService.DeleteReviewAsync(review.Id);
        var reviews = await supportService.GetReviewsByTourIdAsync(tourId);

        // Assert
        Assert.True(deleteResult);
        Assert.Empty(reviews);
    }
}
