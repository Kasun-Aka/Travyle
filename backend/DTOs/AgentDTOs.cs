namespace Travyle.Api.DTOs;

public record StartAgentBookingRequest(
    Guid TravelerId,
    string Objective,
    string? TravelerName,
    string? TravelerEmail,
    Guid? PreferredScheduleId = null,
    DateTime? PreferredDate = null,
    string? PreferredTimeSlot = null,
    int? Guests = null
);

public record ApproveWorkflowRequest(
    string ApproverRole,
    string? ApproverNotes = null,
    string? ApproverUserId = null
);

public record RejectWorkflowRequest(
    string ApproverRole,
    string Reason,
    string? ApproverUserId = null
);

public record ProposedBookingDto(
    Guid ScheduleId,
    string DestinationTitle,
    string Location,
    DateTime BookingDate,
    string TimeSlot,
    int Guests,
    decimal PricePerPerson,
    decimal BasePrice,
    decimal ServiceFee,
    decimal DiscountAmount,
    decimal TotalAmount,
    string PaymentMethod
);

public record AgentWorkflowResponse(
    Guid Id,
    Guid TravelerId,
    string TravelerName,
    string TravelerEmail,
    string Objective,
    string Status,
    List<string> Plan,
    List<string> CompletedSteps,
    Dictionary<string, object?> ToolResults,
    Dictionary<string, bool> ValidationResults,
    ProposedBookingDto? ProposedBooking,
    Guid? CreatedBookingId,
    string? BookingReference,
    string ApprovalStatus,
    string? ApprovedBy,
    string? ApproverRole,
    string? ApproverNotes,
    DateTime? ApprovedAt,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
