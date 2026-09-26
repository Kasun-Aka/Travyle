from fastapi import FastAPI, HTTPException, Header
from datetime import datetime
from .config import settings
from .agents.smart_booking_agent import run_smart_booking_workflow, execute_approved_booking
from .schemas.booking_schemas import StartAgentBookingRequest, AgentWorkflowResponse

app = FastAPI(title="Travyle Smart Booking Agent", version="1.0.0")

@app.get("/health")
async def health_check():
    return {
        "status": "ok",
        "service": "Travyle Smart Booking Agent",
        "framework": "LangGraph + FastAPI",
        "timestamp": datetime.utcnow().isoformat(),
    }

@app.post("/agent/booking/run", response_model=AgentWorkflowResponse)
async def start_booking(request: StartAgentBookingRequest):
    try:
        response = await run_smart_booking_workflow(request)
        return response
    except Exception as e:
        raise HTTPException(status_code=500, detail=str(e))

@app.post("/agent/booking/execute")
async def execute_booking(payload: dict, x_agent_execution_key: str | None = Header(default=None)):
    if not settings.agent_execution_key or x_agent_execution_key != settings.agent_execution_key:
        raise HTTPException(status_code=403, detail="Booking execution requires the trusted backend execution path.")

    try:
        workflow_data = payload.get("workflow", {})
        approver_role = payload.get("approverRole", "")
        approver_user_id = payload.get("approverUserId")
        approver_notes = payload.get("approverNotes")

        result = await execute_approved_booking(
            workflow_data=workflow_data,
            approver_role=approver_role,
            approver_user_id=approver_user_id,
            approver_notes=approver_notes,
        )
        if not result.get("success", False):
            raise HTTPException(status_code=400, detail=result.get("error"))
        return result
    except HTTPException:
        raise
    except Exception:
        raise HTTPException(status_code=500, detail="Booking execution failed safely.")
