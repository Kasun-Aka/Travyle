using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Moq;
using Travyle.Api.Controllers;
using Travyle.Api.DTOs.Operations;
using Travyle.Api.Models;
using Travyle.Api.Repositories;
using Travyle.Api.Services;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Unit tests for Disruption Alert operations in OperationsService.
/// Covers: GetActiveAlertsAsync and CreateDisruptionAlertAsync.
/// </summary>
public class DisruptionAlertServiceTests
{
    private readonly Mock<IOperationsRepository> _repoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly OperationsService _service;

    public DisruptionAlertServiceTests()
    {
        _repoMock = new Mock<IOperationsRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _service = new OperationsService(_repoMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private static DisruptionAlert CreateAlert(
        Guid? id = null,
        Guid? scheduleId = null,
        string type = "Weather",
        string severity = "High",
        string description = "Heavy rainfall expected",
        DateTime? resolvedAt = null)
    {
        return new DisruptionAlert
        {
            Id = id ?? Guid.NewGuid(),
            BookingScheduleId = scheduleId ?? Guid.NewGuid(),
            Type = type,
            Severity = severity,
            Description = description,
            TriggeredAt = DateTime.UtcNow,
            ResolvedAt = resolvedAt
        };
    }

    // ─── GetActiveAlertsAsync ───────────────────────────────────────────────

    [Fact]
    public async Task GetActiveAlerts_NoFilter_ReturnsAllActive()
    {
        // Arrange
        var alerts = new List<DisruptionAlert>
        {
            CreateAlert(type: "Weather", severity: "High"),
            CreateAlert(type: "Traffic", severity: "Medium", description: "Road closure on A9")
        };

        _repoMock.Setup(r => r.GetActiveAlertsAsync(null))
            .ReturnsAsync(alerts);

        // Act
        var result = await _service.GetActiveAlertsAsync(null);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetActiveAlerts_WithBookingScheduleId_FiltersCorrectly()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var matchingAlert = CreateAlert(scheduleId: scheduleId, type: "Weather");

        _repoMock.Setup(r => r.GetActiveAlertsAsync(scheduleId))
            .ReturnsAsync(new List<DisruptionAlert> { matchingAlert });

        // Act
        var result = await _service.GetActiveAlertsAsync(scheduleId);

        // Assert
        Assert.Single(result);
        Assert.Equal(scheduleId, result[0].BookingScheduleId);
    }

    [Fact]
    public async Task GetActiveAlerts_NoAlerts_ReturnsEmptyList()
    {
        // Arrange
        _repoMock.Setup(r => r.GetActiveAlertsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new List<DisruptionAlert>());

        // Act
        var result = await _service.GetActiveAlertsAsync(Guid.NewGuid());

        // Assert
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetActiveAlerts_MapsAllFieldsCorrectly()
    {
        // Arrange
        var alertId = Guid.NewGuid();
        var scheduleId = Guid.NewGuid();
        var triggeredAt = DateTime.UtcNow;
        var alert = new DisruptionAlert
        {
            Id = alertId,
            BookingScheduleId = scheduleId,
            Type = "Traffic",
            Severity = "Critical",
            Description = "Bridge collapse on highway",
            TriggeredAt = triggeredAt,
            ResolvedAt = null
        };

        _repoMock.Setup(r => r.GetActiveAlertsAsync(null))
            .ReturnsAsync(new List<DisruptionAlert> { alert });

        // Act
        var result = await _service.GetActiveAlertsAsync(null);

        // Assert
        Assert.Single(result);
        var dto = result[0];
        Assert.Equal(alertId, dto.Id);
        Assert.Equal(scheduleId, dto.BookingScheduleId);
        Assert.Equal("Traffic", dto.Type);
        Assert.Equal("Critical", dto.Severity);
        Assert.Equal("Bridge collapse on highway", dto.Description);
        Assert.Equal(triggeredAt, dto.TriggeredAt);
        Assert.Null(dto.ResolvedAt);
    }

    [Fact]
    public async Task GetActiveAlerts_ResolvedAlert_IncludesResolvedAt()
    {
        // Arrange
        var resolvedTime = DateTime.UtcNow.AddHours(-1);
        var alert = CreateAlert(resolvedAt: resolvedTime);

        _repoMock.Setup(r => r.GetActiveAlertsAsync(null))
            .ReturnsAsync(new List<DisruptionAlert> { alert });

        // Act
        var result = await _service.GetActiveAlertsAsync(null);

        // Assert
        Assert.Single(result);
        Assert.NotNull(result[0].ResolvedAt);
        Assert.Equal(resolvedTime, result[0].ResolvedAt);
    }

    // ─── CreateDisruptionAlertAsync ─────────────────────────────────────────

    [Fact]
    public async Task CreateDisruptionAlert_ValidInput_ReturnsCreatedDto()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var dto = new CreateDisruptionAlertDto(scheduleId, "Weather", "High", "Severe storm warning");

        _repoMock.Setup(r => r.CreateDisruptionAlertAsync(It.IsAny<DisruptionAlert>()))
            .ReturnsAsync((DisruptionAlert a) => a);

        // Act
        var result = await _service.CreateDisruptionAlertAsync(dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(scheduleId, result.BookingScheduleId);
        Assert.Equal("Weather", result.Type);
        Assert.Equal("High", result.Severity);
        Assert.Equal("Severe storm warning", result.Description);
        Assert.Null(result.ResolvedAt);
    }

    [Fact]
    public async Task CreateDisruptionAlert_TrafficType_SetsCorrectType()
    {
        // Arrange
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Traffic", "Medium", "Road construction on A2");

        _repoMock.Setup(r => r.CreateDisruptionAlertAsync(It.IsAny<DisruptionAlert>()))
            .ReturnsAsync((DisruptionAlert a) => a);

        // Act
        var result = await _service.CreateDisruptionAlertAsync(dto);

        // Assert
        Assert.Equal("Traffic", result.Type);
        Assert.Equal("Medium", result.Severity);
    }

    [Fact]
    public async Task CreateDisruptionAlert_SetsTriggeredAtToUtcNow()
    {
        // Arrange
        var beforeCreate = DateTime.UtcNow;
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Weather", "Low", "Light drizzle");

        _repoMock.Setup(r => r.CreateDisruptionAlertAsync(It.IsAny<DisruptionAlert>()))
            .ReturnsAsync((DisruptionAlert a) => a);

        // Act
        var result = await _service.CreateDisruptionAlertAsync(dto);

        // Assert
        Assert.True(result.TriggeredAt >= beforeCreate);
        Assert.True(result.TriggeredAt <= DateTime.UtcNow.AddSeconds(2));
    }

    [Fact]
    public async Task CreateDisruptionAlert_CallsRepoOnce()
    {
        // Arrange
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Weather", "High", "Tsunami advisory");

        _repoMock.Setup(r => r.CreateDisruptionAlertAsync(It.IsAny<DisruptionAlert>()))
            .ReturnsAsync((DisruptionAlert a) => a);

        // Act
        await _service.CreateDisruptionAlertAsync(dto);

        // Assert
        _repoMock.Verify(r => r.CreateDisruptionAlertAsync(It.Is<DisruptionAlert>(a =>
            a.Type == "Weather" &&
            a.Severity == "High" &&
            a.Description == "Tsunami advisory"
        )), Times.Once);
    }

    [Fact]
    public async Task CreateDisruptionAlert_NewAlertHasNoResolvedAt()
    {
        // Arrange
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Traffic", "Low", "Minor delay");

        _repoMock.Setup(r => r.CreateDisruptionAlertAsync(It.IsAny<DisruptionAlert>()))
            .ReturnsAsync((DisruptionAlert a) => a);

        // Act
        var result = await _service.CreateDisruptionAlertAsync(dto);

        // Assert — newly created alerts should never be resolved
        Assert.Null(result.ResolvedAt);
    }
}

/// <summary>
/// Controller-level tests for disruption alert endpoints in OperationsController.
/// Verifies HTTP status codes and action result types.
/// </summary>
public class DisruptionAlertControllerTests
{
    private readonly Mock<IOperationsService> _serviceMock;
    private readonly OperationsController _controller;

    public DisruptionAlertControllerTests()
    {
        _serviceMock = new Mock<IOperationsService>();

        var dbOptions = new DbContextOptionsBuilder<Travyle.Api.Data.TravyleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new Travyle.Api.Data.TravyleDbContext(dbOptions);

        _controller = new OperationsController(_serviceMock.Object, db);
    }

    // ─── GET /api/operations/disruption-alerts ──────────────────────────────

    [Fact]
    public async Task GetDisruptionAlerts_NoFilter_ReturnsOk()
    {
        // Arrange
        var alerts = new List<DisruptionAlertResponseDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Weather", "High", "Storm", DateTime.UtcNow, null),
            new(Guid.NewGuid(), Guid.NewGuid(), "Traffic", "Medium", "Jam", DateTime.UtcNow, null)
        };

        _serviceMock.Setup(s => s.GetActiveAlertsAsync(null))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetDisruptionAlerts(null);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedAlerts = Assert.IsType<List<DisruptionAlertResponseDto>>(ok.Value);
        Assert.Equal(2, returnedAlerts.Count);
    }

    [Fact]
    public async Task GetDisruptionAlerts_WithScheduleId_ReturnsFilteredOk()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var alerts = new List<DisruptionAlertResponseDto>
        {
            new(Guid.NewGuid(), scheduleId, "Weather", "Critical", "Cyclone approaching", DateTime.UtcNow, null)
        };

        _serviceMock.Setup(s => s.GetActiveAlertsAsync(scheduleId))
            .ReturnsAsync(alerts);

        // Act
        var result = await _controller.GetDisruptionAlerts(scheduleId);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedAlerts = Assert.IsType<List<DisruptionAlertResponseDto>>(ok.Value);
        Assert.Single(returnedAlerts);
        Assert.Equal(scheduleId, returnedAlerts[0].BookingScheduleId);
    }

    [Fact]
    public async Task GetDisruptionAlerts_Empty_ReturnsOkWithEmptyList()
    {
        // Arrange
        _serviceMock.Setup(s => s.GetActiveAlertsAsync(It.IsAny<Guid?>()))
            .ReturnsAsync(new List<DisruptionAlertResponseDto>());

        // Act
        var result = await _controller.GetDisruptionAlerts(Guid.NewGuid());

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        var returnedAlerts = Assert.IsType<List<DisruptionAlertResponseDto>>(ok.Value);
        Assert.Empty(returnedAlerts);
    }

    // ─── POST /api/operations/disruption-alerts ─────────────────────────────

    [Fact]
    public async Task CreateDisruptionAlert_ValidInput_ReturnsCreatedAtAction()
    {
        // Arrange
        var scheduleId = Guid.NewGuid();
        var alertId = Guid.NewGuid();
        var dto = new CreateDisruptionAlertDto(scheduleId, "Weather", "High", "Flash flood warning");
        var responseDto = new DisruptionAlertResponseDto(
            alertId, scheduleId, "Weather", "High", "Flash flood warning", DateTime.UtcNow, null);

        _serviceMock.Setup(s => s.CreateDisruptionAlertAsync(dto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateDisruptionAlert(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task CreateDisruptionAlert_TrafficAlert_ReturnsCreatedAtAction()
    {
        // Arrange
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Traffic", "Low", "Minor congestion");
        var responseDto = new DisruptionAlertResponseDto(
            Guid.NewGuid(), dto.BookingScheduleId, "Traffic", "Low", "Minor congestion", DateTime.UtcNow, null);

        _serviceMock.Setup(s => s.CreateDisruptionAlertAsync(dto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.CreateDisruptionAlert(dto);

        // Assert
        var created = Assert.IsType<CreatedAtActionResult>(result);
        var returnedDto = Assert.IsType<DisruptionAlertResponseDto>(created.Value);
        Assert.Equal("Traffic", returnedDto.Type);
    }

    [Fact]
    public async Task CreateDisruptionAlert_VerifiesServiceCalledOnce()
    {
        // Arrange
        var dto = new CreateDisruptionAlertDto(Guid.NewGuid(), "Weather", "High", "Test alert");
        var responseDto = new DisruptionAlertResponseDto(
            Guid.NewGuid(), dto.BookingScheduleId, "Weather", "High", "Test alert", DateTime.UtcNow, null);

        _serviceMock.Setup(s => s.CreateDisruptionAlertAsync(dto))
            .ReturnsAsync(responseDto);

        // Act
        await _controller.CreateDisruptionAlert(dto);

        // Assert
        _serviceMock.Verify(s => s.CreateDisruptionAlertAsync(dto), Times.Once);
    }
}
