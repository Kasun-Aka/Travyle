using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/support/reviews")]
public class ReviewsController : ControllerBase
{
    private readonly ISupportService _supportService;
    private readonly ILogger<ReviewsController> _logger;

    public ReviewsController(ISupportService supportService, ILogger<ReviewsController> logger)
    {
        _supportService = supportService;
        _logger = logger;
    }

    /// <summary>
    /// Fetch verified traveler ratings/feedback across all tours or specific tour.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetAllReviews(CancellationToken cancellationToken)
    {
        var reviews = await _supportService.GetReviewsByTourIdAsync(null, cancellationToken);
        return Ok(reviews);
    }

    /// <summary>
    /// Fetch verified traveler ratings/feedback for a specific tour or destination.
    /// </summary>
    [HttpGet("{tourId:guid}")]
    public async Task<ActionResult<List<ReviewResponseDto>>> GetReviewsByTour(Guid tourId, CancellationToken cancellationToken)
    {
        var reviews = await _supportService.GetReviewsByTourIdAsync(tourId, cancellationToken);
        return Ok(reviews);
    }

    /// <summary>
    /// Traveler submits a review and rating for a tour.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<ReviewResponseDto>> CreateReview([FromBody] CreateReviewDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var review = await _supportService.CreateReviewAsync(dto, cancellationToken);
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating customer review");
            return StatusCode(500, new { message = "An error occurred while submitting review." });
        }
    }

    /// <summary>
    /// Toggle verification status for a customer review.
    /// </summary>
    [HttpPut("{id:guid}/verify")]
    public async Task<ActionResult<ReviewResponseDto>> ToggleVerification(Guid id, [FromQuery] bool isVerified = true, CancellationToken cancellationToken = default)
    {
        try
        {
            var review = await _supportService.ToggleReviewVerificationAsync(id, isVerified, cancellationToken);
            if (review == null)
            {
                return NotFound(new { message = $"Review with ID {id} was not found." });
            }
            return Ok(review);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error toggling verification for review {ReviewId}", id);
            return StatusCode(500, new { message = "An error occurred while updating review verification status." });
        }
    }

    /// <summary>
    /// Delete a customer review.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteReview(Guid id, CancellationToken cancellationToken)
    {
        var success = await _supportService.DeleteReviewAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Review with ID {id} was not found." });
        }
        return NoContent();
    }
}
