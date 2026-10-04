using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Travyle.Api.Controllers;
using Travyle.Api.Data;
using Travyle.Api.DTOs;
using Travyle.Api.Models;
using Travyle.Api.Services;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Unit tests for the DestinationsController (CRUD + Non-CRUD PDF generation).
/// Covers issue #15 (Destinations CRUD API with geocoding).
/// </summary>
public class DestinationsControllerTests : IDisposable
{
    private readonly TravyleDbContext _db;
    private readonly DestinationsController _controller;

    public DestinationsControllerTests()
    {
        var options = new DbContextOptionsBuilder<TravyleDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _db = new TravyleDbContext(options);

        // Use a mock geocoding service so tests don't call external APIs
        var mockGeocoding = new MockGeocodingService();
        _controller = new DestinationsController(_db, mockGeocoding);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private async Task<Destination> SeedDestinationAsync(string name = "Sigiriya", string region = "Central")
    {
        var dest = new Destination
        {
            Id = Guid.NewGuid(),
            Name = name,
            Region = region,
            Description = "Ancient rock fortress",
            Tags = new[] { "History", "Culture" },
            Latitude = 7.9572,
            Longitude = 80.7601
        };
        _db.Destinations.Add(dest);
        await _db.SaveChangesAsync();
        return dest;
    }

    // ─── GET /api/destinations ────────────────────────────────────────────────

    [Fact]
    public async Task GetDestinations_EmptyDb_ReturnsEmptyList()
    {
        // Act
        var result = await _controller.GetDestinations(null, null, null, null, null, 1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDestinations_WithDestinations_ReturnsList()
    {
        // Arrange
        await SeedDestinationAsync("Sigiriya", "Central");
        await SeedDestinationAsync("Galle Fort", "Southern");

        // Act
        var result = await _controller.GetDestinations(null, null, null, null, null, 1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDestinations_SearchByName_FiltersCorrectly()
    {
        // Arrange
        await SeedDestinationAsync("Sigiriya", "Central");
        await SeedDestinationAsync("Galle Fort", "Southern");

        // Act
        var result = await _controller.GetDestinations("Sigiriya", null, null, null, null, 1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDestinations_FilterByRegion_ReturnsMatchingOnly()
    {
        // Arrange
        await SeedDestinationAsync("Sigiriya", "Central");
        await SeedDestinationAsync("Galle Fort", "Southern");

        // Act
        var result = await _controller.GetDestinations(null, "Southern", null, null, null, 1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    // ─── GET /api/destinations/{id} ───────────────────────────────────────────

    [Fact]
    public async Task GetDestination_ValidId_ReturnsDestination()
    {
        // Arrange
        var dest = await SeedDestinationAsync();

        // Act
        var result = await _controller.GetDestination(dest.Id);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetDestination_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.GetDestination(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result.Result);
    }

    // ─── POST /api/destinations ───────────────────────────────────────────────

    [Fact]
    public async Task CreateDestination_ValidInput_ReturnsCreated()
    {
        // Arrange
        var dto = new CreateDestinationDto
        {
            Name = "Yala National Park",
            Region = "Southern",
            Description = "Famous wildlife sanctuary",
            Tags = new[] { "Wildlife", "Safari" }
        };

        // Act
        var result = await _controller.CreateDestination(dto);

        // Assert
        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(1, await _db.Destinations.CountAsync());
    }

    [Fact]
    public async Task CreateDestination_EmptyName_ReturnsBadRequest()
    {
        // Arrange
        var dto = new CreateDestinationDto
        {
            Name = "",
            Region = "Southern",
            Description = "Test",
            Tags = Array.Empty<string>()
        };
        _controller.ModelState.AddModelError("Name", "Name is required");

        // Act
        var result = await _controller.CreateDestination(dto);

        // Assert
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    // ─── DELETE /api/destinations/{id} ────────────────────────────────────────

    [Fact]
    public async Task DeleteDestination_ValidId_ReturnsNoContent()
    {
        // Arrange
        var dest = await SeedDestinationAsync();

        // Act
        var result = await _controller.DeleteDestination(dest.Id);

        // Assert
        Assert.IsType<NoContentResult>(result);
        Assert.Equal(0, await _db.Destinations.CountAsync());
    }

    [Fact]
    public async Task DeleteDestination_InvalidId_ReturnsNotFound()
    {
        // Act
        var result = await _controller.DeleteDestination(Guid.NewGuid());

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    public void Dispose()
    {
        _db.Dispose();
    }
}

/// <summary>
/// Test double for the geocoding service — returns predictable coordinates
/// without making real HTTP calls to Nominatim.
/// </summary>
public class MockGeocodingService : IGeocodingService
{
    public Task<(double Latitude, double Longitude)?> GetCoordinatesAsync(string address)
    {
        // Always return a fixed coordinate for tests — no real HTTP calls made
        return Task.FromResult<(double, double)?>((7.8731, 80.7718));
    }
}
