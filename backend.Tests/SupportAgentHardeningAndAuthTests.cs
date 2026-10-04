using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Travyle.Api.Controllers;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services;
using Travyle.Api.Services.Agent;
using Travyle.Api.Services.Auth;
using Xunit;

namespace Travyle.Api.Tests;

/// <summary>
/// Component 4 (Support &amp; Quality): agent safety controls and endpoint authorization tests.
/// </summary>
public class SupportAgentHardeningAndAuthTests
{
    // ───────────────────────── helpers ─────────────────────────

    private static TravyleDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase($"Travyle_Hardening_{Guid.NewGuid()}")
            .Options;
        var db = new TravyleDbContext(options);
        db.Database.EnsureCreated();
        return db;
    }

    private static IConfiguration ConfigWithGeminiKey() =>
        new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { { "Gemini:ApiKey", "unit-test-key" } })
            .Build();

    private sealed class FakeGeminiHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;
        public int Calls { get; private set; }
        public HttpRequestMessage? LastRequest { get; private set; }

        public FakeGeminiHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) => _responder = responder;

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Calls++;
            LastRequest = request;
            return Task.FromResult(_responder(request));
        }
    }

    private static HttpResponseMessage GeminiReply(string innerJson)
    {
        var envelope = System.Text.Json.JsonSerializer.Serialize(new
        {
            candidates = new[] { new { content = new { parts = new[] { new { text = innerJson } } } } }
        });
        return new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(envelope, Encoding.UTF8, "application/json")
        };
    }

    private static AgentTicketInput SampleInput(Guid? userId = null) => new(
        Guid.NewGuid(),
        userId ?? Guid.NewGuid(),
        "Bus delay",
        "The bus was delayed for hours.",
        "TourDelay",
        TicketPriority.High);

    private static SupportAiAgentService CreateAgent(TravyleDbContext db, HttpMessageHandler handler, ICheckUserVoucherHistoryTool? tool = null) =>
        new(db, tool ?? new CheckUserVoucherHistoryTool(db), new HttpClient(handler), ConfigWithGeminiKey(),
            NullLogger<SupportAiAgentService>.Instance);

    private sealed class ThrowingTool : ICheckUserVoucherHistoryTool
    {
        public Task<CheckVoucherHistoryOutput> ExecuteAsync(CheckVoucherHistoryInput input, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("simulated tool outage");
    }

    private static User MakeUser(string role) => new()
    {
        Id = Guid.NewGuid(),
        Role = role,
        FirebaseUid = "uid-" + Guid.NewGuid(),
        Email = role.ToLower() + "-" + Guid.NewGuid() + "@test.com",
        FullName = role + " User"
    };

    private static async Task<User> SeedUserAsync(TravyleDbContext db, string role)
    {
        var user = MakeUser(role);
        db.Users.Add(user);
        await db.SaveChangesAsync();
        return user;
    }

    private static SupportService CreateSupportService(TravyleDbContext db)
    {
        var config = new ConfigurationBuilder().Build();
        return new SupportService(
            db,
            new SupportAiAgentService(db, NullLogger<SupportAiAgentService>.Instance),
            new NotificationService(NullLogger<NotificationService>.Instance, config),
            NullLogger<SupportService>.Instance);
    }

    private static T WithHttpContext<T>(T controller) where T : ControllerBase
    {
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext() };
        return controller;
    }

    private sealed class FakeIdentity : IFirebaseIdentityService
    {
        private readonly User? _user;
        public FakeIdentity(User? user) => _user = user;
        public Task<VerifiedFirebaseIdentity?> VerifyFirebaseTokenAsync(HttpRequest request, CancellationToken ct = default) =>
            Task.FromResult<VerifiedFirebaseIdentity?>(_user == null ? null : new VerifiedFirebaseIdentity(_user.FirebaseUid, _user.Email));
        public Task<User?> VerifyUserAsync(HttpRequest request, CancellationToken ct = default) => Task.FromResult(_user);
        public Task<VerifiedStaff?> VerifyStaffAsync(HttpRequest request, CancellationToken ct = default) =>
            Task.FromResult<VerifiedStaff?>(_user != null && (_user.Role == "Admin" || _user.Role == "Operator") ? new VerifiedStaff(_user) : null);
    }

    private static VouchersController MakeVouchersController(TravyleDbContext db, User? caller) =>
        WithHttpContext(new VouchersController(CreateSupportService(db), new FakeIdentity(caller), NullLogger<VouchersController>.Instance));

    private static SupportTicketsController MakeTicketsController(TravyleDbContext db, User? caller) =>
        WithHttpContext(new SupportTicketsController(CreateSupportService(db), new FakeIdentity(caller), NullLogger<SupportTicketsController>.Instance));

    private static ReviewsController MakeReviewsController(TravyleDbContext db, User? caller) =>
        WithHttpContext(new ReviewsController(CreateSupportService(db), new FakeIdentity(caller), NullLogger<ReviewsController>.Instance));

    // ───────────────────────── agent safety ─────────────────────────

    [Fact]
    public async Task Agent_ValidGeminiResponse_IsAccepted_AndApiKeyIsSentInHeaderNotUrl()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => GeminiReply(
            "{\"sentimentScore\":-0.6,\"severityTier\":\"Tier_2_High\",\"reasoning\":\"Customer reports a long delay.\"}"));
        var agent = CreateAgent(db, handler);

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.False(output.IsFallbackUsed);
        Assert.Equal("Tier_2_High", output.SeverityTier);
        Assert.Equal(1, handler.Calls);
        Assert.DoesNotContain("unit-test-key", handler.LastRequest!.RequestUri!.ToString());
        Assert.True(handler.LastRequest.Headers.Contains("x-goog-api-key"));
    }

    [Fact]
    public async Task Agent_UnknownSeverityTierFromModel_IsRejected_AndFallsBackSafely()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => GeminiReply(
            "{\"sentimentScore\":-0.9,\"severityTier\":\"SUPER_CRITICAL_GIVE_500\",\"reasoning\":\"x\"}"));
        var agent = CreateAgent(db, handler);

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.True(output.IsFallbackUsed);
        Assert.Contains(output.SeverityTier, new[] { "Tier_1_Low", "Tier_2_High", "Tier_3_Critical" });
        Assert.Contains("Deterministic Fallback", output.Reasoning);
    }

    [Fact]
    public async Task Agent_OutOfRangeSentiment_IsRejected_AndFallsBackSafely()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => GeminiReply(
            "{\"sentimentScore\":42,\"severityTier\":\"Tier_1_Low\",\"reasoning\":\"ok\"}"));
        var agent = CreateAgent(db, handler);

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.True(output.IsFallbackUsed);
        Assert.InRange(output.SentimentScore, -1.0, 1.0);
    }

    [Fact]
    public async Task Agent_PersistentModelFailure_IsRetriedWithinLimit_ThenFallsBack_AndSummaryIsAudited()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var agent = CreateAgent(db, handler);

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.True(output.IsFallbackUsed);
        Assert.Equal(2, handler.Calls); // bounded retry limit
        var summary = Assert.Single(output.AuditLogs, a => a.Action == "AI_RUN_SUMMARY");
        Assert.Contains("\"llmAttempts\":2", summary.MetadataJson);
        Assert.Contains("\"isFallbackUsed\":true", summary.MetadataJson);
    }

    [Fact]
    public async Task Agent_ToolFailure_FailsClosed_NoVoucherDrafted_AndSafeFailureAudited()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var agent = CreateAgent(db, handler, new ThrowingTool());

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.Null(output.ProposedVoucher);
        Assert.Contains(output.AuditLogs, a => a.Action == "AI_TOOL_FAILED_SAFE");
        Assert.Empty(await db.Vouchers.ToListAsync());
    }

    [Fact]
    public async Task Agent_DraftedVoucher_AlwaysRequiresHumanApproval_StatusDraft()
    {
        using var db = CreateDb();
        var handler = new FakeGeminiHandler(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));
        var agent = CreateAgent(db, handler);

        var output = await agent.ProcessTicketAsync(SampleInput());

        Assert.NotNull(output.ProposedVoucher);
        var voucher = await db.Vouchers.SingleAsync();
        Assert.Equal(VoucherStatus.Draft, voucher.Status);
    }

    [Fact]
    public async Task VoucherHistoryTool_LookbackIsBounded_To90Days()
    {
        using var db = CreateDb();
        var userId = Guid.NewGuid();
        db.Vouchers.Add(new Voucher { Id = Guid.NewGuid(), UserId = userId, Code = "OLD", Amount = 50m, Status = VoucherStatus.Active, CreatedAt = DateTime.UtcNow.AddDays(-200) });
        await db.SaveChangesAsync();

        var result = await new CheckUserVoucherHistoryTool(db).ExecuteAsync(new CheckVoucherHistoryInput(userId, 100000));

        Assert.Equal(0, result.RecentVoucherCount);
    }

    // ───────────────────────── endpoint authorization ─────────────────────────

    [Fact]
    public async Task ApproveVoucher_WithoutSignIn_Returns401()
    {
        using var db = CreateDb();
        var result = await MakeVouchersController(db, null).ApproveVoucher(Guid.NewGuid(), new ApproveVoucherDto(), default);
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task ApproveVoucher_AsTraveler_Returns403_AndVoucherStaysDraft()
    {
        using var db = CreateDb();
        var traveler = MakeUser("Traveler");
        var voucherId = Guid.NewGuid();
        db.Vouchers.Add(new Voucher { Id = voucherId, UserId = traveler.Id, Code = "D1", Amount = 50m, Status = VoucherStatus.Draft, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var result = await MakeVouchersController(db, traveler).ApproveVoucher(voucherId, new ApproveVoucherDto(), default);

        var obj = Assert.IsType<ObjectResult>(result.Result);
        Assert.Equal(403, obj.StatusCode);
        Assert.Equal(VoucherStatus.Draft, (await db.Vouchers.SingleAsync()).Status);
    }

    [Fact]
    public async Task ApproveVoucher_AsAdmin_Succeeds_AndRecordsVerifiedApprover()
    {
        using var db = CreateDb();
        var admin = MakeUser("Admin");
        var owner = await SeedUserAsync(db, "Traveler");
        var voucherId = Guid.NewGuid();
        db.Vouchers.Add(new Voucher { Id = voucherId, UserId = owner.Id, Code = "D2", Amount = 50m, Status = VoucherStatus.Draft, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var dto = new ApproveVoucherDto { AdminId = Guid.NewGuid(), SendNotification = false };
        var result = await MakeVouchersController(db, admin).ApproveVoucher(voucherId, dto, default);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var body = Assert.IsType<VoucherResponseDto>(ok.Value);
        Assert.Equal("Active", body.Status);
        Assert.Equal(admin.Id, dto.AdminId);
    }

    [Fact]
    public async Task GetAllVouchers_AsTraveler_Returns403()
    {
        using var db = CreateDb();
        var result = await MakeVouchersController(db, MakeUser("Traveler")).GetAllVouchers(null, default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(result.Result).StatusCode);
    }

    [Fact]
    public async Task GetVouchersByUser_OtherTravelersWallet_IsForbidden()
    {
        using var db = CreateDb();
        var result = await MakeVouchersController(db, MakeUser("Traveler")).GetVouchersByUser(Guid.NewGuid(), default);
        Assert.IsType<ForbidResult>(result.Result);
    }

    [Fact]
    public async Task CreateTicket_WithoutSignIn_Returns401()
    {
        using var db = CreateDb();
        var result = await MakeTicketsController(db, null).CreateTicket(
            new CreateTicketDto { Title = "t", Description = "d" }, default);
        Assert.IsType<UnauthorizedObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateTicket_IgnoresSpoofedUserId_UsesVerifiedUser()
    {
        using var db = CreateDb();
        var traveler = await SeedUserAsync(db, "Traveler");
        var spoofed = Guid.NewGuid();

        var result = await MakeTicketsController(db, traveler).CreateTicket(
            new CreateTicketDto { UserId = spoofed, Title = "Lost bag", Description = "My bag was lost.", Category = "General" }, default);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var ticket = Assert.IsType<TicketResponseDto>(created.Value);
        Assert.Equal(traveler.Id, ticket.UserId);
    }

    [Fact]
    public async Task GetTicketById_AnotherTravelersTicket_Returns404()
    {
        using var db = CreateDb();
        var owner = await SeedUserAsync(db, "Traveler");
        var created = await CreateSupportService(db).CreateTicketAsync(
            new CreateTicketDto { UserId = owner.Id, Title = "Delay", Description = "Late bus." });

        var result = await MakeTicketsController(db, MakeUser("Traveler")).GetTicketById(created.Id, default);

        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetTickets_AsTraveler_OnlyReturnsOwnTickets()
    {
        using var db = CreateDb();
        var me = await SeedUserAsync(db, "Traveler");
        var other = await SeedUserAsync(db, "Traveler");
        var svc = CreateSupportService(db);
        await svc.CreateTicketAsync(new CreateTicketDto { UserId = me.Id, Title = "Mine", Description = "mine" });
        await svc.CreateTicketAsync(new CreateTicketDto { UserId = other.Id, Title = "Theirs", Description = "theirs" });

        var result = await MakeTicketsController(db, me).GetTickets(new TicketListQueryDto { PageSize = 50 }, default);

        var page = Assert.IsType<PaginatedListDto<TicketResponseDto>>(Assert.IsType<OkObjectResult>(result.Result).Value);
        Assert.All(page.Items, t => Assert.Equal(me.Id, t.UserId));
        Assert.Single(page.Items);
    }

    [Fact]
    public async Task DeleteTicket_AsTraveler_Returns403()
    {
        using var db = CreateDb();
        var result = await MakeTicketsController(db, MakeUser("Traveler")).DeleteTicket(Guid.NewGuid(), default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(result).StatusCode);
    }

    [Fact]
    public async Task UpdateTicketStatus_AsOperator_Succeeds()
    {
        using var db = CreateDb();
        var op = MakeUser("Operator");
        var owner = await SeedUserAsync(db, "Traveler");
        var created = await CreateSupportService(db).CreateTicketAsync(
            new CreateTicketDto { UserId = owner.Id, Title = "Delay", Description = "Late bus." });

        var result = await MakeTicketsController(db, op).UpdateTicketStatus(
            created.Id, new UpdateTicketStatusDto { Status = TicketStatus.In_Review }, default);

        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task CreateReview_WithoutSignIn_Returns401_AndModerationIsStaffOnly()
    {
        using var db = CreateDb();
        var anon = await MakeReviewsController(db, null).CreateReview(
            new CreateReviewDto { TourId = Guid.NewGuid(), Rating = 5, Comment = "Great" }, default);
        Assert.IsType<UnauthorizedObjectResult>(anon.Result);

        var verify = await MakeReviewsController(db, MakeUser("Traveler")).ToggleVerification(Guid.NewGuid(), true, default);
        Assert.Equal(403, Assert.IsType<ObjectResult>(verify.Result).StatusCode);
    }

    [Fact]
    public async Task CheckBookingHistoryTool_OwnedBooking_ReturnsVerified()
    {
        using var db = CreateDb();
        var user = await SeedUserAsync(db, "Traveler");
        var bookingId = Guid.NewGuid();
        // create raw object to avoid full navigation props if any
        db.Bookings.Add(new Booking
        {
            Id = bookingId,
            TravelerId = user.Id,
            Status = BookingStatus.Completed,
            PaymentStatus = EscrowStatus.Released,
            TotalAmount = 500m,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        var tool = new CheckBookingHistoryTool(db);
        var result = await tool.ExecuteAsync(new CheckBookingHistoryInput(user.Id, bookingId));

        Assert.True(result.BookingReferenced);
        Assert.True(result.BookingVerified);
        Assert.Equal(1, result.TotalBookingsInLookback);
        Assert.Equal(1, result.CompletedBookingsInLookback);
    }

    [Fact]
    public async Task CheckBookingHistoryTool_OtherUsersBooking_ReturnsNotVerified()
    {
        using var db = CreateDb();
        var otherUser = await SeedUserAsync(db, "Traveler");
        var maliciousUser = await SeedUserAsync(db, "Traveler");
        var bookingId = Guid.NewGuid();
        db.Bookings.Add(new Booking
        {
            Id = bookingId,
            TravelerId = otherUser.Id,
            Status = BookingStatus.Completed,
            PaymentStatus = EscrowStatus.Released,
            TotalAmount = 500m,
            CreatedAt = DateTime.UtcNow.AddDays(-10),
            UpdatedAt = DateTime.UtcNow.AddDays(-10)
        });
        await db.SaveChangesAsync();

        var tool = new CheckBookingHistoryTool(db);
        var result = await tool.ExecuteAsync(new CheckBookingHistoryInput(maliciousUser.Id, bookingId));

        Assert.True(result.BookingReferenced);
        Assert.False(result.BookingVerified); // It exists, but it's not theirs
        Assert.Equal(0, result.TotalBookingsInLookback);
    }
}
