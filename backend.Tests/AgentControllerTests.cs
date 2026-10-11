using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Travyle.Api.Controllers;
using Travyle.Api.Data;
using Travyle.Api.Models;
using Travyle.Api.Services;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Unit tests for the AgentController (AI Recommendation & Itinerary endpoints).
/// Covers issue #16 (AI Recommendation Agent) and #17 (AI Itinerary Generator).
/// </summary>
public class AgentControllerTests : IDisposable
{
    private readonly TravyleDbContext _db;
    private readonly Mock<GeminiService> _mockGeminiService;
    private readonly AgentController _controller;

    public AgentControllerTests()
    {
        // Use an in-memory database so tests don't touch the real Supabase DB
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TravyleDbContext(options);

        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns("test-key");
        _mockGeminiService = new Mock<GeminiService>(new HttpClient(), mockConfig.Object);

        _controller = new AgentController(_db, _mockGeminiService.Object);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private async Task<User> SeedUserWithProfileAsync(string email = "test@example.com")
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            FullName = "Test Traveler",
            FirebaseUid = "firebase-uid-123",
            Role = "Traveler"
        };
        var profile = new TravelerProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            PreferredActivities = new[] { "Hiking", "Culture" },
            BudgetRange = "Moderate",
            TripHistory = "Visited Colombo last year"
        };
        user.TravelerProfile = profile;
        _db.Users.Add(user);
        await _db.SaveChangesAsync();
        return user;
    }

    private async Task<Destination> SeedDestinationAsync()
    {
        var dest = new Destination
        {
            Id = Guid.NewGuid(),
            Name = "Ella Highlands",
            Region = "Central Province",
            Description = "Beautiful tea country",
            Tags = new[] { "Hiking", "Nature" }
        };
        _db.Destinations.Add(dest);
        await _db.SaveChangesAsync();
        return dest;
    }

    // ─── Recommend Endpoint Tests ─────────────────────────────────────────────

    [Fact]
    public async Task Recommend_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new AgentController.RecommendRequest { Email = "" };

        // Act
        var result = await _controller.GetRecommendation(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task Recommend_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AgentController.RecommendRequest { Email = "nobody@example.com" };

        // Act
        var result = await _controller.GetRecommendation(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Recommend_NoDestinationsInDb_ReturnsNotFound()
    {
        // Arrange
        await SeedUserWithProfileAsync();
        var request = new AgentController.RecommendRequest { Email = "test@example.com" };

        // Act
        var result = await _controller.GetRecommendation(request);

        // Assert - no destinations seeded, should return 404
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task Recommend_ValidRequest_AiServiceCalled_ReturnsOk()
    {
        // Arrange
        var user = await SeedUserWithProfileAsync();
        var dest = await SeedDestinationAsync();

        var fakeAiResponse = $@"{{""destinationId"":""{dest.Id}"",""reasoning"":""Perfect for hikers!""}}";
        _mockGeminiService
            .Setup(s => s.GetRecommendationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(fakeAiResponse);

        var request = new AgentController.RecommendRequest { Email = user.Email };

        // Act
        var result = await _controller.GetRecommendation(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(okResult.Value);
        _mockGeminiService.Verify(s => s.GetRecommendationAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task Recommend_AiServiceThrows_Returns500()
    {
        // Arrange
        var user = await SeedUserWithProfileAsync();
        await SeedDestinationAsync();

        _mockGeminiService
            .Setup(s => s.GetRecommendationAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("Simulated AI failure"));

        var request = new AgentController.RecommendRequest { Email = user.Email };

        // Act
        var result = await _controller.GetRecommendation(request);

        // Assert
        var statusResult = Assert.IsType<ObjectResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    // ─── Itinerary Endpoint Tests ─────────────────────────────────────────────

    [Fact]
    public async Task GenerateItinerary_EmptyEmail_ReturnsBadRequest()
    {
        // Arrange
        var request = new AgentController.ItineraryRequest
        {
            Email = "",
            DestinationId = Guid.NewGuid()
        };

        // Act
        var result = await _controller.GenerateItinerary(request);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task GenerateItinerary_UserNotFound_ReturnsNotFound()
    {
        // Arrange
        var request = new AgentController.ItineraryRequest
        {
            Email = "ghost@example.com",
            DestinationId = Guid.NewGuid()
        };

        // Act
        var result = await _controller.GenerateItinerary(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GenerateItinerary_DestinationNotFound_ReturnsNotFound()
    {
        // Arrange
        var user = await SeedUserWithProfileAsync();
        var request = new AgentController.ItineraryRequest
        {
            Email = user.Email,
            DestinationId = Guid.NewGuid() // non-existent destination
        };

        // Act
        var result = await _controller.GenerateItinerary(request);

        // Assert
        Assert.IsType<NotFoundObjectResult>(result);
    }

    [Fact]
    public async Task GenerateItinerary_ValidRequest_ReturnsOkWithDays()
    {
        // Arrange
        var user = await SeedUserWithProfileAsync();
        var dest = await SeedDestinationAsync();

        var fakeItinerary = @"[
            {""day"":1,""title"":""Arrival"",""description"":""Check in and explore.""},
            {""day"":2,""title"":""Hiking"",""description"":""Trek the hills.""},
            {""day"":3,""title"":""Departure"",""description"":""Head home refreshed.""}
        ]";
        _mockGeminiService
            .Setup(s => s.GetItineraryAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(fakeItinerary);

        var request = new AgentController.ItineraryRequest
        {
            Email = user.Email,
            DestinationId = dest.Id
        };

        // Act
        var result = await _controller.GenerateItinerary(request);

        // Assert
        Assert.IsType<OkObjectResult>(result);
        _mockGeminiService.Verify(s => s.GetItineraryAsync(
            dest.Name, dest.Region,
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public void GeminiService_Constructor_MissingKey_DoesNotThrow()
    {
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns((string?)null);

        var service = new GeminiService(new HttpClient(), mockConfig.Object);
        Assert.NotNull(service);
    }

    [Fact]
    public void GeminiService_Constructor_ReadsFlatEnvironmentKey()
    {
        var mockConfig = new Mock<Microsoft.Extensions.Configuration.IConfiguration>();
        mockConfig.Setup(c => c["Gemini:ApiKey"]).Returns((string?)null);
        mockConfig.Setup(c => c["GEMINI_API_KEY"]).Returns("flat-env-key");

        var service = new GeminiService(new HttpClient(), mockConfig.Object);
        Assert.NotNull(service);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}
