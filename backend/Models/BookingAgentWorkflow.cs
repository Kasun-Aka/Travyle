using System.ComponentModel.DataAnnotations;

namespace Travyle.Api.Models;

public enum AgentWorkflowStatus
{
    Running,
    PendingApproval,
    Approved,
    Rejected,
    Completed,
    Failed
}

public class BookingAgentWorkflow
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid TravelerId { get; set; }
    public string TravelerName { get; set; } = string.Empty;
    public string TravelerEmail { get; set; } = string.Empty;

    public string Objective { get; set; } = string.Empty;

    public AgentWorkflowStatus Status { get; set; } = AgentWorkflowStatus.Running;

    // Execution details stored as JSON strings
    public string PlanJson { get; set; } = "[]";
    public string CompletedStepsJson { get; set; } = "[]";
    public string ToolResultsJson { get; set; } = "{}";
    public string ValidationResultsJson { get; set; } = "{}";

    // Booking Proposal & Execution
    public string? ProposedBookingJson { get; set; }
    public Guid? CreatedBookingId { get; set; }
    public string? BookingReference { get; set; }

    // Approval details
    public string ApprovalStatus { get; set; } = "PENDING"; // PENDING, APPROVED, REJECTED
    public string? ApprovedBy { get; set; }
    public string? ApproverRole { get; set; }
    public string? ApproverNotes { get; set; }
    public DateTime? ApprovedAt { get; set; }

    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
