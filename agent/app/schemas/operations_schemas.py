from pydantic import BaseModel, Field
from typing import List, Optional

class MonitorOperationsRequest(BaseModel):
    booking_schedule_id: str
    lat: float
    lon: float
    
class MonitorOperationsResponse(BaseModel):
    status: str
    message: str
    disruption_alert_id: Optional[str] = None
    rerouted: bool = False
    optimized_route: Optional[List[dict]] = None
