using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services;
using Travyle.Api.Services.Auth;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/support/tickets")]
public class SupportTicketsController : SupportControllerBase
{
    private readonly ISupportService _supportService;
    private readonly ILogger<SupportTicketsController> _logger;

    public SupportTicketsController(
        ISupportService supportService,
        IFirebaseIdentityService identityService,
        ILogger<SupportTicketsController> logger) : base(identityService)
    {
        _supportService = supportService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new support ticket / dispute. Initiates automated AI sentiment & severity triage.
    /// The ticket owner is always the verified signed-in user (client-supplied UserId is ignored).
    /// </summary>
    [HttpPost]
    public async Task<ActionResult<TicketResponseDto>> CreateTicket([FromBody] CreateTicketDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.UserId = user.Id;

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
    /// Staff see all tickets; travelers only see their own.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<PaginatedListDto<TicketResponseDto>>> GetTickets([FromQuery] TicketListQueryDto query, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        if (!IsStaff(user))
        {
            query.UserId = user.Id;
        }

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
    /// Only the ticket owner or staff may view it.
    /// </summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<TicketResponseDto>> GetTicketById(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        var ticket = await _supportService.GetTicketByIdAsync(id, cancellationToken);
        if (ticket == null || (ticket.UserId != user.Id && !IsStaff(user)))
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }
        return Ok(ticket);
    }

    /// <summary>
    /// Update ticket details (Title, Description, Category, Priority). Allowed in early states.
    /// Only the ticket owner or staff may update it.
    /// </summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<TicketResponseDto>> UpdateTicket(Guid id, [FromBody] UpdateTicketDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var existing = await _supportService.GetTicketByIdAsync(id, cancellationToken);
        if (existing == null || (existing.UserId != user.Id && !IsStaff(user)))
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }

        try
        {
            var result = await _supportService.UpdateTicketAsync(id, dto, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    /// <summary>
    /// Update ticket status or assign admin resolution summary. Staff only; the acting admin is the verified user.
    /// </summary>
    [HttpPut("{id:guid}/status")]
    public async Task<ActionResult<TicketResponseDto>> UpdateTicketStatus(Guid id, [FromBody] UpdateTicketStatusDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        dto.AdminId = user.Id;

        var result = await _supportService.UpdateTicketStatusAsync(id, dto, cancellationToken);
        if (result == null)
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }
        return Ok(result);
    }

    /// <summary>
    /// Soft delete a support ticket. Staff only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> DeleteTicket(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        var success = await _supportService.DeleteTicketAsync(id, cancellationToken);
        if (!success)
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }
        return NoContent();
    }

    /// <summary>
    /// Non-CRUD business operation: Evaluates ticket sentiment score, auto-classifies severity tier, checks policy, and issues instant system audit entries.
    /// Only the ticket owner or staff may trigger it.
    /// </summary>
    [HttpPost("{id:guid}/auto-resolve-claim")]
    public async Task<ActionResult<AutoResolveResultDto>> AutoResolveClaim(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        var existing = await _supportService.GetTicketByIdAsync(id, cancellationToken);
        if (existing == null || (existing.UserId != user.Id && !IsStaff(user)))
        {
            return NotFound(new { message = $"Ticket with ID {id} was not found." });
        }

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

    /// <summary>
    /// Upload a photo attachment (incident photo evidence) for a support ticket.
    /// Returns the accessible public image URL. Requires sign-in; only JPG/PNG/WEBP images up to 5 MB are accepted.
    /// </summary>
    [HttpPost("upload-attachment")]
    [Consumes("multipart/form-data")]
    public async Task<ActionResult<object>> UploadAttachment([FromForm] UploadTicketAttachmentDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        if (dto.File == null || dto.File.Length == 0)
        {
            return BadRequest(new { message = "Please select a valid image file to upload." });
        }

        try
        {
            var url = await _supportService.UploadAttachmentAsync(dto.File, Request, cancellationToken);
            return Ok(new { url });
        }
        catch (ArgumentException ex)
        {
            // Validation failure (type / size / not a real image): the client can fix this.
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading ticket attachment photo");
            return StatusCode(500, new { message = "An error occurred while saving the attachment image." });
        }
    }

    /// <summary>
    /// Feature 1: Retrieve analytics stats strip data (avg sentiment score, active/redeemed vouchers, tour ratings summary). Staff only.
    /// </summary>
    [HttpGet("/api/support/analytics")]
    public async Task<ActionResult<SupportAnalyticsDto>> GetAnalytics(CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        try
        {
            var analytics = await _supportService.GetAnalyticsAsync(cancellationToken);
            return Ok(analytics);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching support analytics");
            return StatusCode(500, new { message = "An error occurred while retrieving support analytics." });
        }
    }

    /// <summary>
    /// Feature 2: Retrieve user support activity (tickets + reviews joined by UserId). Staff or user.
    /// </summary>
    [HttpGet("/api/support/users/{userId:guid}/activity")]
    public async Task<ActionResult<UserSupportActivityDto>> GetUserActivity(Guid userId, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (user.Id != userId && !IsStaff(user)) return Forbid();

        try
        {
            var activity = await _supportService.GetUserActivityAsync(userId, cancellationToken);
            return Ok(activity);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching user support activity for user {UserId}", userId);
            return StatusCode(500, new { message = "An error occurred while retrieving user support activity." });
        }
    }

    /// <summary>
    /// Feature 1 (Mobile): Cancel ticket (soft-delete via status change to Closed). Allowed only for ticket owner in Pending_AI_Triage or In_Review state.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    public async Task<ActionResult<TicketResponseDto>> CancelTicket(Guid id, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        try
        {
            var result = await _supportService.CancelTicketAsync(id, user.Id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error cancelling support ticket {TicketId}", id);
            return StatusCode(500, new { message = "An error occurred while cancelling the ticket." });
        }
    }

    /// <summary>
    /// Feature 2 (Mobile): Attach an append-only follow-up note to an existing ticket without modifying original fields.
    /// Must verify the requesting user owns the ticket.
    /// </summary>
    [HttpPost("{id:guid}/followup")]
    public async Task<ActionResult<TicketResponseDto>> AddFollowupNote(Guid id, [FromBody] CreateFollowupDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            var result = await _supportService.AddFollowupNoteAsync(id, user.Id, dto.Note, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding follow-up note to ticket {TicketId}", id);
            return StatusCode(500, new { message = "An error occurred while adding the follow-up note." });
        }
    }

    /// <summary>
    /// Feature 1: Admin explicitly reviews/edits and sends the AI-drafted reply message to the traveler.
    /// Dispatches email notification via SendGrid and logs an ADMIN_REPLY_SENT audit entry. Staff only.
    /// </summary>
    [HttpPost("{id:guid}/send-reply")]
    public async Task<ActionResult<TicketResponseDto>> SendCustomerReply(Guid id, [FromBody] SendTicketReplyDto dto, CancellationToken cancellationToken)
    {
        var user = await GetSignedInUserAsync(cancellationToken);
        if (user == null) return SignInRequired();
        if (!IsStaff(user)) return StaffOnly();

        if (!ModelState.IsValid || string.IsNullOrWhiteSpace(dto.ReplyMessage))
        {
            return BadRequest(new { message = "Reply message cannot be empty." });
        }

        try
        {
            var result = await _supportService.SendCustomerReplyAsync(id, dto.ReplyMessage, user.Id, cancellationToken);
            if (result == null)
            {
                return NotFound(new { message = $"Ticket with ID {id} was not found." });
            }
            return Ok(result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending customer reply for ticket {TicketId}", id);
            return StatusCode(500, new { message = "An error occurred while sending the customer reply." });
        }
    }
}
