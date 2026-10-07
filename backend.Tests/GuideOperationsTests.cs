using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Travyle.Api.Controllers;
using Travyle.Api.DTOs.Operations;
using Travyle.Api.Models;
using Travyle.Api.Repositories;
using Travyle.Api.Services;
using Xunit;

namespace Travyle.Tests;

/// <summary>
/// Unit tests for Guide-related operations in OperationsService and OperationsController.
/// Covers: UpdateGuideAssignment, GetAvailableGuides (filter + pagination).
/// </summary>
public class GuideOperationsServiceTests
{
    private readonly Mock<IOperationsRepository> _repoMock;
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
    private readonly Mock<IConfiguration> _configMock;
    private readonly OperationsService _service;

    public GuideOperationsServiceTests()
    {
        _repoMock = new Mock<IOperationsRepository>();
        _httpClientFactoryMock = new Mock<IHttpClientFactory>();
        _configMock = new Mock<IConfiguration>();
        _service = new OperationsService(_repoMock.Object, _httpClientFactoryMock.Object, _configMock.Object);
    }

    // ─── Seed helpers ────────────────────────────────────────────────────────

    private static GuideAssignment CreateGuideAssignment(
        Guid? id = null,
        Guid? scheduleId = null,
        Guid? guideUserId = null,
        string status = "Assigned")
    {
        return new GuideAssignment
        {
            Id = id ?? Guid.NewGuid(),
            BookingScheduleId = scheduleId ?? Guid.NewGuid(),
            GuideUserId = guideUserId ?? Guid.NewGuid(),
            Status = status,
            AssignedAt = DateTime.UtcNow,
            Guide = new User
            {
                Id = guideUserId ?? Guid.NewGuid(),
                FullName = "Test Guide",
                Email = "guide@test.com",
                Role = "Local Guide"
            }
        };
    }

    // ─── UpdateGuideAssignmentAsync ──────────────────────────────────────────

    [Fact]
    public async Task UpdateGuideAssignment_ValidId_ReturnsUpdatedDto()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var existing = CreateGuideAssignment(id: assignmentId, status: "Assigned");
        var dto = new UpdateGuideAssignmentDto(null, "Active");

        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(assignmentId))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()))
            .ReturnsAsync((GuideAssignment ga) => ga);

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(assignmentId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Active", result!.Status);
        Assert.Equal(assignmentId, result.Id);
    }

    [Fact]
    public async Task UpdateGuideAssignment_NonExistentId_ReturnsNull()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();
        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(nonExistentId))
            .ReturnsAsync((GuideAssignment?)null);

        var dto = new UpdateGuideAssignmentDto(null, "Active");

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(nonExistentId, dto);

        // Assert
        Assert.Null(result);
        _repoMock.Verify(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()), Times.Never);
    }

    [Fact]
    public async Task UpdateGuideAssignment_UpdateGuideUserId_ChangesGuide()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var newGuideId = Guid.NewGuid();
        var existing = CreateGuideAssignment(id: assignmentId);
        var dto = new UpdateGuideAssignmentDto(newGuideId, null);

        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(assignmentId))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()))
            .ReturnsAsync((GuideAssignment ga) => ga);

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(assignmentId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newGuideId, result!.GuideUserId);
    }

    [Fact]
    public async Task UpdateGuideAssignment_EmptyStatus_DoesNotOverwrite()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var existing = CreateGuideAssignment(id: assignmentId, status: "Active");
        var dto = new UpdateGuideAssignmentDto(null, ""); // empty string — should not overwrite

        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(assignmentId))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()))
            .ReturnsAsync((GuideAssignment ga) => ga);

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(assignmentId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Active", result!.Status); // status should remain unchanged
    }

    [Fact]
    public async Task UpdateGuideAssignment_BothFields_UpdatesBoth()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var newGuideId = Guid.NewGuid();
        var existing = CreateGuideAssignment(id: assignmentId, status: "Assigned");
        var dto = new UpdateGuideAssignmentDto(newGuideId, "Completed");

        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(assignmentId))
            .ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()))
            .ReturnsAsync((GuideAssignment ga) => ga);

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(assignmentId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(newGuideId, result!.GuideUserId);
        Assert.Equal("Completed", result.Status);
    }

    // ─── GetAvailableGuidesAsync ────────────────────────────────────────────

    [Fact]
    public async Task GetAvailableGuides_NoFilters_ReturnsPaginatedResult()
    {
        // Arrange
        var guides = new List<GuideAssignment>
        {
            CreateGuideAssignment(status: "Assigned"),
            CreateGuideAssignment(status: "Assigned")
        };

        _repoMock.Setup(r => r.GetAvailableGuidesAsync(null, null, 1, 20))
            .ReturnsAsync((guides, 2));

        // Act
        var result = await _service.GetAvailableGuidesAsync(null, null, 1, 20);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.TotalCount);
        Assert.Equal(2, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task GetAvailableGuides_WithLocationFilter_PassesFilterToRepo()
    {
        // Arrange
        var guides = new List<GuideAssignment> { CreateGuideAssignment() };

        _repoMock.Setup(r => r.GetAvailableGuidesAsync("Colombo", null, 1, 10))
            .ReturnsAsync((guides, 1));

        // Act
        var result = await _service.GetAvailableGuidesAsync("Colombo", null, 1, 10);

        // Assert
        Assert.Single(result.Items);
        _repoMock.Verify(r => r.GetAvailableGuidesAsync("Colombo", null, 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetAvailableGuides_WithLanguageFilter_PassesFilterToRepo()
    {
        // Arrange
        var guides = new List<GuideAssignment> { CreateGuideAssignment() };

        _repoMock.Setup(r => r.GetAvailableGuidesAsync(null, "English", 1, 10))
            .ReturnsAsync((guides, 1));

        // Act
        var result = await _service.GetAvailableGuidesAsync(null, "English", 1, 10);

        // Assert
        Assert.Single(result.Items);
        _repoMock.Verify(r => r.GetAvailableGuidesAsync(null, "English", 1, 10), Times.Once);
    }

    [Fact]
    public async Task GetAvailableGuides_EmptyResults_ReturnsEmptyPaginatedResult()
    {
        // Arrange
        _repoMock.Setup(r => r.GetAvailableGuidesAsync(It.IsAny<string?>(), It.IsAny<string?>(), 1, 10))
            .ReturnsAsync((new List<GuideAssignment>(), 0));

        // Act
        var result = await _service.GetAvailableGuidesAsync("NonExistentLocation", null, 1, 10);

        // Assert
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
    }

    [Fact]
    public async Task GetAvailableGuides_SecondPage_CorrectPagination()
    {
        // Arrange
        var guides = new List<GuideAssignment> { CreateGuideAssignment() };

        _repoMock.Setup(r => r.GetAvailableGuidesAsync(null, null, 2, 5))
            .ReturnsAsync((guides, 6)); // total 6, page 2 has 1 item

        // Act
        var result = await _service.GetAvailableGuidesAsync(null, null, 2, 5);

        // Assert
        Assert.Single(result.Items);
        Assert.Equal(6, result.TotalCount);
        Assert.Equal(2, result.Page);
        Assert.Equal(5, result.PageSize);
    }

    // ─── Response DTO mapping ───────────────────────────────────────────────

    [Fact]
    public async Task UpdateGuideAssignment_MapsGuideFullNameCorrectly()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var guideId = Guid.NewGuid();
        var existing = new GuideAssignment
        {
            Id = assignmentId,
            BookingScheduleId = Guid.NewGuid(),
            GuideUserId = guideId,
            Status = "Assigned",
            AssignedAt = DateTime.UtcNow,
            Guide = new User
            {
                Id = guideId,
                FullName = "Kamal Perera",
                Email = "kamal@test.com",
                Role = "Local Guide"
            }
        };
        var dto = new UpdateGuideAssignmentDto(null, "Active");

        _repoMock.Setup(r => r.GetGuideAssignmentByIdAsync(assignmentId)).ReturnsAsync(existing);
        _repoMock.Setup(r => r.UpdateGuideAssignmentAsync(It.IsAny<GuideAssignment>()))
            .ReturnsAsync((GuideAssignment ga) => ga);

        // Act
        var result = await _service.UpdateGuideAssignmentAsync(assignmentId, dto);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Kamal Perera", result!.GuideFullName);
        Assert.Equal(guideId, result.GuideUserId);
    }
}

/// <summary>
/// Controller-level tests for guide operations endpoints in OperationsController.
/// Verifies HTTP status codes and action result types.
/// </summary>
public class GuideOperationsControllerTests
{
    private readonly Mock<IOperationsService> _serviceMock;
    private readonly OperationsController _controller;

    public GuideOperationsControllerTests()
    {
        _serviceMock = new Mock<IOperationsService>();

        var dbOptions = new DbContextOptionsBuilder<Travyle.Api.Data.TravyleDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new Travyle.Api.Data.TravyleDbContext(dbOptions);

        _controller = new OperationsController(_serviceMock.Object, db);
    }

    // ─── PUT /api/operations/assignments/{id} ───────────────────────────────

    [Fact]
    public async Task UpdateGuideAssignment_ValidId_ReturnsOk()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var dto = new UpdateGuideAssignmentDto(null, "Active");
        var responseDto = new GuideAssignmentResponseDto(
            assignmentId, Guid.NewGuid(), Guid.NewGuid(), "Test Guide", "Active", DateTime.UtcNow);

        _serviceMock.Setup(s => s.UpdateGuideAssignmentAsync(assignmentId, dto))
            .ReturnsAsync(responseDto);

        // Act
        var result = await _controller.UpdateGuideAssignment(assignmentId, dto);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task UpdateGuideAssignment_NonExistentId_ReturnsNotFound()
    {
        // Arrange
        var assignmentId = Guid.NewGuid();
        var dto = new UpdateGuideAssignmentDto(null, "Active");

        _serviceMock.Setup(s => s.UpdateGuideAssignmentAsync(assignmentId, dto))
            .ReturnsAsync((GuideAssignmentResponseDto?)null);

        // Act
        var result = await _controller.UpdateGuideAssignment(assignmentId, dto);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    // ─── GET /api/operations/guides/available ───────────────────────────────

    [Fact]
    public async Task GetAvailableGuides_DefaultParams_ReturnsOk()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<GuideAssignmentResponseDto>(
            new List<GuideAssignmentResponseDto>(), 0, 1, 20);

        _serviceMock.Setup(s => s.GetAvailableGuidesAsync(null, null, 1, 20))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetAvailableGuides(null, null, 1, 20);

        // Assert
        Assert.IsType<OkObjectResult>(result);
    }

    [Fact]
    public async Task GetAvailableGuides_WithFilters_ReturnsOk()
    {
        // Arrange
        var paginatedResult = new PaginatedResult<GuideAssignmentResponseDto>(
            new List<GuideAssignmentResponseDto>
            {
                new(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "Guide A", "Assigned", DateTime.UtcNow)
            }, 1, 1, 10);

        _serviceMock.Setup(s => s.GetAvailableGuidesAsync("Kandy", "Sinhala", 1, 10))
            .ReturnsAsync(paginatedResult);

        // Act
        var result = await _controller.GetAvailableGuides("Kandy", "Sinhala", 1, 10);

        // Assert
        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
    }
}
