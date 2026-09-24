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
}
