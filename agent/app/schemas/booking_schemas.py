from pydantic import BaseModel, Field, ConfigDict
from typing import List, Dict, Optional, Any
from uuid import UUID
from datetime import datetime

class StartAgentBookingRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    traveler_id: UUID = Field(..., alias="TravelerId")
    objective: str = Field(..., min_length=1, max_length=2000, alias="Objective")
    traveler_name: Optional[str] = Field(None, alias="TravelerName")
    traveler_email: Optional[str] = Field(None, alias="TravelerEmail")
    preferred_schedule_id: Optional[UUID] = Field(None, alias="PreferredScheduleId")
    preferred_date: Optional[datetime] = Field(None, alias="PreferredDate")
    preferred_time_slot: Optional[str] = Field(None, alias="PreferredTimeSlot")
    guests: Optional[int] = Field(None, alias="Guests")

class ProposedBookingDto(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    schedule_id: UUID = Field(..., alias="ScheduleId")
    destination_title: str = Field(..., alias="DestinationTitle")
    location: str = Field(..., alias="Location")
    booking_date: datetime = Field(..., alias="BookingDate")
    time_slot: str = Field(..., alias="TimeSlot")
    guests: int = Field(..., alias="Guests")
    price_per_person: float = Field(..., alias="PricePerPerson")
    base_price: float = Field(..., alias="BasePrice")
    service_fee: float = Field(..., alias="ServiceFee")
    discount_amount: float = Field(..., alias="DiscountAmount")
    total_amount: float = Field(..., alias="TotalAmount")
    payment_method: str = Field(..., alias="PaymentMethod")

class ExecuteWorkflowRequest(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    workflow_id: UUID = Field(..., alias="WorkflowId")
    approver_role: str = Field(..., alias="ApproverRole")
    approver_user_id: Optional[str] = Field(None, alias="ApproverUserId")
    approver_notes: Optional[str] = Field(None, alias="ApproverNotes")

class AgentWorkflowResponse(BaseModel):
    model_config = ConfigDict(populate_by_name=True)

    id: UUID = Field(..., alias="Id")
    traveler_id: UUID = Field(..., alias="TravelerId")
    traveler_name: str = Field(..., alias="TravelerName")
    traveler_email: str = Field(..., alias="TravelerEmail")
    objective: str = Field(..., alias="Objective")
    status: str = Field(..., alias="Status")
    plan: List[str] = Field(default_factory=list, alias="Plan")
    completed_steps: List[str] = Field(default_factory=list, alias="CompletedSteps")
    tool_results: Dict[str, Any] = Field(default_factory=dict, alias="ToolResults")
    validation_results: Dict[str, bool] = Field(default_factory=dict, alias="ValidationResults")
    proposed_booking: Optional[ProposedBookingDto] = Field(None, alias="ProposedBooking")
    created_booking_id: Optional[UUID] = Field(None, alias="CreatedBookingId")
    booking_reference: Optional[str] = Field(None, alias="BookingReference")
    approval_status: str = Field(..., alias="ApprovalStatus")
    approved_by: Optional[str] = Field(None, alias="ApprovedBy")
    approver_role: Optional[str] = Field(None, alias="ApproverRole")
    approver_notes: Optional[str] = Field(None, alias="ApproverNotes")
    approved_at: Optional[datetime] = Field(None, alias="ApprovedAt")
    error_message: Optional[str] = Field(None, alias="ErrorMessage")
    created_at: datetime = Field(..., alias="CreatedAt")
    updated_at: datetime = Field(..., alias="UpdatedAt")
