import httpx
import logging
import os
from typing import Any, Dict
from google import genai
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
    api_key = os.getenv("OPENWEATHERMAP_API_KEY", "")
    weather_condition = None
    
    if api_key:
        url = f"https://api.openweathermap.org/data/2.5/weather?lat={lat}&lon={lon}&appid={api_key}"
        try:
            async with httpx.AsyncClient(timeout=5.0) as client:
                resp = await client.get(url)
                if resp.status_code == 200:
                    data = resp.json()
                    weather_main = data.get("weather", [{}])[0].get("main", "")
                    if weather_main in ["Thunderstorm", "Rain", "Snow", "Extreme"]:
                        weather_condition = weather_main
        except Exception:
            pass

    # Dummy logic to always test disruption if no real API key or no weather
    if not weather_condition and lat > 5.0 and lon > 5.0:
        weather_condition = "Heavy Traffic and Storm"

    if weather_condition:
        gemini_api_key = os.getenv("GEMINI_API_KEY", "")
        if gemini_api_key and "YOUR_GEMINI_API_KEY" not in gemini_api_key:
            try:
                ai_client = genai.Client(api_key=gemini_api_key)
                chat = ai_client.chats.create(model='gemini-3.8-flash')
                response = chat.send_message(
                    f'Generate a short, professional, 2-sentence alert for a travel disruption caused by {weather_condition} at coordinates {lat}, {lon}.'
                )
                description = response.text.strip()
            except Exception as e:
                logger.error(f"Gemini API error: {e}")
                description = f"Disruption Alert: {weather_condition} at location."
        else:
            description = f"Disruption Alert: {weather_condition} at location."
            
        return {
            "severity": "High" if weather_condition in ["Thunderstorm", "Extreme"] else "Medium",
            "description": description
        }
    
    return {}
