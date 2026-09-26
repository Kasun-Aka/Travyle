using Microsoft.AspNetCore.Mvc;
using Travyle.Api.DTOs;
using Travyle.Api.Services.Agent;
using Travyle.Api.Services.Auth;

namespace Travyle.Api.Controllers;

[ApiController]
[Route("api/agent")]
[Produces("application/json")]
public class SmartBookingAgentController : ControllerBase
{
    private readonly ISmartBookingAgentService _agentService;
    private readonly IFirebaseIdentityService _identityService;

    public SmartBookingAgentController(
        ISmartBookingAgentService agentService,
        IFirebaseIdentityService identityService)
    {
        _agentService = agentService;
        _identityService = identityService;
    }

    /// <summary>
    /// Starts a new smart booking workflow. Evaluates intent, queries tools, validates deterministically,
    /// and pauses at PENDING_APPROVAL.
    /// </summary>
    [HttpPost("bookings/start")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> StartBookingWorkflow(
        [FromBody] StartAgentBookingRequest request,
        CancellationToken ct)
    {
        if (request.TravelerId == Guid.Empty || string.IsNullOrWhiteSpace(request.Objective))
        {
            return BadRequest(new { error = "TravelerId and Objective are required." });
        }

        if (request.Objective.Length > 2000)
        {
            return BadRequest(new { error = "Objective must not exceed 2000 characters." });
        }

        var response = await _agentService.StartWorkflowAsync(request, ct);
        return Ok(response);
    }

    /// <summary>
    /// Retrieves a workflow by its unique ID.
    /// </summary>
    [HttpGet("workflows/{workflowId:guid}")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkflow(Guid workflowId, CancellationToken ct)
    {
        var response = await _agentService.GetWorkflowByIdAsync(workflowId, ct);
        return response == null ? NotFound(new { error = $"Workflow {workflowId} not found." }) : Ok(response);
    }

    /// <summary>
    /// Returns all workflows created by a specific traveler.
    /// </summary>
    [HttpGet("workflows/traveler/{travelerId:guid}")]
    [ProducesResponseType(typeof(IEnumerable<AgentWorkflowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTravelerWorkflows(Guid travelerId, CancellationToken ct)
    {
        var response = await _agentService.GetTravelerWorkflowsAsync(travelerId, ct);
        return Ok(response);
    }

    /// <summary>
    /// Returns all workflows currently awaiting operator/admin approval.
    /// </summary>
    [HttpGet("workflows/pending")]
    [ProducesResponseType(typeof(IEnumerable<AgentWorkflowResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPendingWorkflows(CancellationToken ct)
    {
        var response = await _agentService.GetPendingWorkflowsAsync(ct);
        return Ok(response);
    }

    /// <summary>
    /// Approves a pending booking proposal. Requires Admin or Operator role.
    /// Executes the final booking deterministically.
    /// </summary>
    [HttpPost("workflows/{workflowId:guid}/approve")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ApproveWorkflow(
        Guid workflowId,
        [FromBody] ApproveWorkflowRequest request,
        CancellationToken ct)
    {
        var staff = await _identityService.VerifyStaffAsync(Request, ct);
        if (staff == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "A verified Admin or Operator Firebase account is required." });

        request = request with
        {
            ApproverRole = staff.User.Role,
            ApproverUserId = staff.User.Id.ToString()
        };

        var (result, error) = await _agentService.ApproveWorkflowAsync(workflowId, request, ct);
        if (error != null)
        {
            if (error.StartsWith("Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error });
            }
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error });
            }
            return BadRequest(new { error, workflow = result });
        }

        return Ok(result);
    }

    /// <summary>
    /// Rejects a pending booking proposal with a specified reason. Requires Admin or Operator role.
    /// </summary>
    [HttpPost("workflows/{workflowId:guid}/reject")]
    [ProducesResponseType(typeof(AgentWorkflowResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RejectWorkflow(
        Guid workflowId,
        [FromBody] RejectWorkflowRequest request,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
        {
            return BadRequest(new { error = "Reason is required." });
        }

        var staff = await _identityService.VerifyStaffAsync(Request, ct);
        if (staff == null)
            return StatusCode(StatusCodes.Status403Forbidden, new { error = "A verified Admin or Operator Firebase account is required." });

        request = request with
        {
            ApproverRole = staff.User.Role,
            ApproverUserId = staff.User.Id.ToString()
        };

        var (result, error) = await _agentService.RejectWorkflowAsync(workflowId, request, ct);
        if (error != null)
        {
            if (error.StartsWith("Unauthorized", StringComparison.OrdinalIgnoreCase))
            {
                return StatusCode(StatusCodes.Status403Forbidden, new { error });
            }
            if (error.Contains("not found", StringComparison.OrdinalIgnoreCase))
            {
                return NotFound(new { error });
            }
            return BadRequest(new { error });
        }

        return Ok(result);
    }
}
