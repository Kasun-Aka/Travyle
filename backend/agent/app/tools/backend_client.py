import httpx
from typing import Any, Dict, List, Optional, Tuple
from ..config import settings

class BackendClient:
    """Controlled client for calling ASP.NET Core IBookingAgentTools endpoints.

    Ensures:
    - Real database access via existing Travyle backend
    - No direct database connections from Python
    - No arbitrary SQL execution
    - Robust error handling and timeout safety
    """

    def __init__(self, base_url: str | None = None):
        self.base_url = (base_url or settings.backend_base_url).rstrip("/")

    def _get_headers(self) -> Dict[str, str]:
        headers = {"Content-Type": "application/json"}
        if settings.backend_api_key:
            headers["X-API-Key"] = settings.backend_api_key
            headers["X-Agent-Tools-Key"] = settings.backend_api_key
        return headers

    async def check_traveler(self, traveler_id: str) -> Dict[str, Any]:
        """Check whether traveler exists in Travyle database."""
        url = f"{self.base_url}/api/agent/tools/traveler/{traveler_id}"
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return {"exists": False}
            except Exception as ex:
                return {"exists": False, "error": str(ex)}

    async def get_available_booking_schedules(
        self, destination_query: Optional[str] = None, filter_date: Optional[str] = None
    ) -> List[Dict[str, Any]]:
        """Tool 1: get_available_booking_schedules."""
        url = f"{self.base_url}/api/agent/tools/schedules"
        params = {}
        if destination_query:
            params["query"] = destination_query
        if filter_date:
            params["date"] = filter_date

        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, params=params, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return []
            except Exception:
                return []

    async def get_booking_schedule_details(self, schedule_id: str) -> Optional[Dict[str, Any]]:
        """Tool 2: get_booking_schedule_details."""
        url = f"{self.base_url}/api/agent/tools/schedules/{schedule_id}"
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return None
            except Exception:
                return None

    async def check_booking_capacity(
        self, schedule_id: str, date: str, time_slot: str, requested_guests: int
    ) -> Dict[str, Any]:
        """Tool 3: check_booking_capacity."""
        url = f"{self.base_url}/api/agent/tools/check-capacity"
        payload = {
            "ScheduleId": schedule_id,
            "Date": date,
            "TimeSlot": time_slot,
            "Guests": requested_guests,
        }
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.post(url, json=payload, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return {
                    "isAvailable": False,
                    "message": "Capacity service unavailable or error returned.",
                }
            except Exception as ex:
                return {"isAvailable": False, "message": f"Connection error: {str(ex)}"}

    async def get_traveler_bookings(self, traveler_id: str) -> List[Dict[str, Any]]:
        """Tool 4: get_traveler_bookings."""
        url = f"{self.base_url}/api/agent/tools/traveler-bookings/{traveler_id}"
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return []
            except Exception:
                return []

    async def calculate_booking_summary(
        self, schedule_id: str, guests: int
    ) -> Optional[Dict[str, Any]]:
        """Tool 5: calculate_booking_summary."""
        url = f"{self.base_url}/api/agent/tools/calculate-summary"
        payload = {"ScheduleId": schedule_id, "Guests": guests}
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.post(url, json=payload, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return None
            except Exception:
                return None

    async def create_booking(self, request_payload: Dict[str, Any]) -> Tuple[Optional[Dict[str, Any]], Optional[str]]:
        """Tool 6: create_booking."""
        url = f"{self.base_url}/api/agent/tools/create-booking"
        async with httpx.AsyncClient(timeout=15.0) as client:
            try:
                resp = await client.post(url, json=request_payload, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json(), None
                error_body = resp.json() if resp.headers.get("content-type", "").startswith("application/json") else resp.text
                err_msg = error_body.get("error", str(error_body)) if isinstance(error_body, dict) else str(error_body)
                return None, err_msg
            except Exception as ex:
                return None, f"Connection error executing booking: {str(ex)}"

    async def get_booking_status(self, booking_id: str) -> Optional[Dict[str, Any]]:
        """Tool 7: get_booking_status."""
        url = f"{self.base_url}/api/agent/tools/bookings/{booking_id}"
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return None
            except Exception:
                return None

    async def get_agent_workflow(self, workflow_id: str) -> Optional[Dict[str, Any]]:
        url = f"{self.base_url}/api/agent/workflows/{workflow_id}"
        async with httpx.AsyncClient(timeout=10.0) as client:
            try:
                resp = await client.get(url, headers=self._get_headers())
                if resp.status_code == 200:
                    return resp.json()
                return None
            except Exception:
                return None

backend_client = BackendClient()
