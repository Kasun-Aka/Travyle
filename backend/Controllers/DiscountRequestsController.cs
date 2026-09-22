using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

/// <summary>
/// POST /api/discount-requests – submit a discount request for a booking
/// </summary>
[ApiController]
[Route("api/discount-requests")]
[Produces("application/json")]
public class DiscountRequestsController : ControllerBase
{
    private readonly IDiscountRequestService _service;

    public DiscountRequestsController(IDiscountRequestService service)
    {
        _service = service;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<DiscountRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        return Ok(await _service.GetAllDiscountRequestsAsync(ct));
    }

    [HttpGet("traveler/{travelerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<DiscountRequestResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetForTraveler(Guid travelerId, CancellationToken ct)
    {
        return Ok(await _service.GetTravelerDiscountRequestsAsync(travelerId, ct));
    }

    [HttpPut("{id:guid}/status")]
    [ProducesResponseType(typeof(DiscountRequestResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateBookingStatusRequest request, CancellationToken ct)
    {
        var result = await _service.UpdateDiscountRequestStatusAsync(id, request.Status, ct);
        return result == null ? NotFound() : Ok(result);
    }

    /// <summary>
    /// Submits a discount request for an existing booking.
    /// All discount requests remain pending until an administrator approves or rejects them.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(DiscountRequestResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create([FromBody] CreateDiscountRequestRequest request, CancellationToken ct)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var (result, error) = await _service.CreateDiscountRequestAsync(request, ct);
        if (error != null)
            return UnprocessableEntity(new { error });

        return StatusCode(StatusCodes.Status201Created, result);
    }
}
