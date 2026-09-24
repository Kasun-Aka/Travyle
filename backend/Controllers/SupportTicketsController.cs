using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/support/tickets")]
public class SupportTicketsController : ControllerBase
{
    private readonly ISupportService _supportService;
    private readonly ILogger<SupportTicketsController> _logger;

    public SupportTicketsController(ISupportService supportService, ILogger<SupportTicketsController> logger)
    {
        _supportService = supportService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new support ticket / dispute. Initiates automated AI sentiment & severity triage.
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketResponseDto>> CreateTicket([FromBody] CreateTicketDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _supportService.CreateTicketAsync(dto, cancellationToken);
            return CreatedAtAction(nameof(GetTicketById), new { id = result.Id }, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating support ticket");
            return StatusCode(500, new { message = "An error occurred while creating the support ticket." });
        }
    }

    /// <summary>
    /// Paginated list of support tickets filtered by priority, status, and search query.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedListDto<TicketResponseDto>>> GetTickets([FromQuery] TicketListQueryDto query, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _supportService.GetTicketsAsync(query, cancellationToken);
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching support tickets");
            return StatusCode(500, new { message = "An error occurred while retrieving tickets." });
        }
    }

    /// <summary>
    /// Retrieve single ticket detail, complete with AI triage results, full audit trace, and linked vouchers.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketResponseDto>> GetTicketById(Guid id, CancellationToken cancellationToken)
    {
        var ticket = await _supportService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket == null)
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }
        return Ok(ticket);
    }

    /// <summary>
    /// Update ticket status or assign admin resolution summary.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<TicketResponseDto>> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusDto dto, CancellationToken cancellationToken)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _supportService.UpdateTicketStatusAsync(id, dto, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Non-CRUD business operation: Evaluates ticket sentiment score, auto-classifies severity tier, checks policy, and issues instant system audit entries.
    /// </summary>
    [HttpPost("{id:guid}/auto-resolve-claim")]
    public async Task<ActionResult<AutoResolveResultDto>> AutoResolveClaim(Guid id, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _supportService.AutoResolveClaimAsync(id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error performing auto-resolve claim for ticket {TicketId}", id);
            return StatusCode(500, new { message = "Failed to run AI triage on the requested claim." });
        }
    }
}
