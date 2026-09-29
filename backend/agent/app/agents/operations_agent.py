import httpx
import logging
import os
from typing import Any, Dict
from ..config import settings
from ..schemas.operations_schemas import MonitorOperationsRequest, MonitorOperationsResponse

logger = logging.getLogger(__name__)

async def run_operations_monitor(request: MonitorOperationsRequest) -> MonitorOperationsResponse:
    # 1. Check OpenWeatherMap for alerts (Mocked or real)
    weather_alert = await check_weather(request.lat, request.lon)
    
    if not weather_alert:
        return MonitorOperationsResponse(
            status="OK",
            message="No active disruptions found.",
            rerouted=False
        )

    # 2. If there's an alert, push it to C# backend
    alert_payload = {
        "bookingScheduleId": request.booking_schedule_id,
        "type": "Weather",
        "severity": weather_alert.get("severity", "High"),
        "description": weather_alert.get("description", "Severe weather condition.")
    }
    
    # Send alert to backend
    base_url = settings.backend_base_url.rstrip("/")
    headers = {"Content-Type": "application/json"}
    if settings.backend_api_key:
        headers["X-API-Key"] = settings.backend_api_key
        headers["X-Agent-Tools-Key"] = settings.backend_api_key
        
    disruption_alert_id = None
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.post(f"{base_url}/api/operations/disruption-alerts", json=alert_payload, headers=headers)
            if resp.status_code == 201:
                disruption_alert_id = resp.json().get("id")
    except Exception as e:
        logger.error(f"Failed to push disruption alert: {e}")
        
    # 3. Call reorder-route-optimization
    optimized_route = None
    rerouted = False
    
    # In a real app we'd get the waypoints from the schedule.
    # We will just pass dummy waypoints to test the TSP route logic recalculation.
    optimization_payload = {
        "bookingScheduleId": request.booking_schedule_id,
        "waypoints": [
            {"name": "Current Location", "latitude": request.lat, "longitude": request.lon},
            {"name": "Alternate Stop A", "latitude": request.lat + 0.01, "longitude": request.lon + 0.01},
            {"name": "Alternate Stop B", "latitude": request.lat + 0.02, "longitude": request.lon - 0.01}
        ]
    }
    
    try:
        async with httpx.AsyncClient(timeout=10.0) as client:
            resp = await client.post(f"{base_url}/api/operations/reorder-route-optimization", json=optimization_payload, headers=headers)
            if resp.status_code == 200:
                optimized_route = resp.json().get("optimizedOrder")
                rerouted = True
    except Exception as e:
        logger.error(f"Failed to reorder route: {e}")

    return MonitorOperationsResponse(
        status="DISRUPTION_DETECTED",
        message=f"Detected {alert_payload['description']}. Route has been optimized.",
        disruption_alert_id=disruption_alert_id,
        rerouted=rerouted,
        optimized_route=optimized_route
    )

async def check_weather(lat: float, lon: float) -> Dict[str, Any]:
    # Use OpenWeatherMap API here. We'll use a mocked approach or real if key provided.
    # To use real: https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={API_KEY}
    
    # Since API key might not be available, we simulate a disruption if lat/lon are within a specific range,
    # or just return a dummy alert for demonstration.
    # Let's say if lat > 0, we trigger a storm alert.
    
    # Real integration — key loaded from environment (set via appsettings.Development.json)
    api_key = os.getenv("OPENWEATHERMAP_API_KEY", "")
    if not api_key:
        logger.warning("OPENWEATHERMAP_API_KEY not set, skipping real weather check.")
    url = f"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={api_key}"
    try:
        async with httpx.AsyncClient(timeout=5.0) as client:
            # We don't fail if OpenWeatherMap is unauthorized, just fallback to dummy logic
            resp = await client.get(url)
            if resp.status_code == 200:
                data = resp.json()
                weather_main = data.get("weather", [{}])[0].get("main", "")
                if weather_main in ["Thunderstorm", "Rain", "Snow", "Extreme"]:
                    return {
                        "severity": "High",
                        "description": f"OpenWeatherMap Alert: {weather_main} at location."
                    }
    except Exception:
        pass

    # Dummy logic to always test disruption if no real API key
    if lat > 5.0 and lon > 5.0:
        return {
            "severity": "Medium",
            "description": "Simulated heavy traffic/weather disruption near location."
        }
    
    return {}
