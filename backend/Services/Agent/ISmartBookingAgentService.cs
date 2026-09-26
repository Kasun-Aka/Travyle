using Travyle.Api.DTOs;

namespace Travyle.Api.Services.Agent;

public interface ISmartBookingAgentService
{
    Task<AgentWorkflowResponse> StartWorkflowAsync(
        StartAgentBookingRequest request,
        CancellationToken ct = default);

    Task<AgentWorkflowResponse?> GetWorkflowByIdAsync(
        Guid workflowId,
        CancellationToken ct = default);

    Task<IEnumerable<AgentWorkflowResponse>> GetTravelerWorkflowsAsync(
        Guid travelerId,
        CancellationToken ct = default);

    Task<IEnumerable<AgentWorkflowResponse>> GetPendingWorkflowsAsync(
        CancellationToken ct = default);

    Task<(AgentWorkflowResponse? Workflow, string? Error)> ApproveWorkflowAsync(
        Guid workflowId,
        ApproveWorkflowRequest request,
        CancellationToken ct = default);

    Task<(AgentWorkflowResponse? Workflow, string? Error)> RejectWorkflowAsync(
        Guid workflowId,
        RejectWorkflowRequest request,
        CancellationToken ct = default);
}
