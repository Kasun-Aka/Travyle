import pytest
import uuid
import httpx
from unittest.mock import AsyncMock, patch
from app.schemas.operations_schemas import MonitorOperationsRequest
from app.agents.operations_agent import run_operations_monitor

# Using pytest-asyncio to test async functions
@pytest.mark.asyncio
async def test_run_operations_monitor_no_disruption():
    # Arrange
    req = MonitorOperationsRequest(
        booking_schedule_id=str(uuid.uuid4()),
        lat=1.0, # Below 5.0 so dummy logic won't trigger disruption
        lon=1.0
    )

    # Act
    res = await run_operations_monitor(req)

    # Assert
    assert res.status == "OK"
    assert "No active disruptions" in res.message
    assert res.rerouted is False
    assert res.disruption_alert_id is None

@pytest.mark.asyncio
@patch("app.agents.operations_agent.httpx.AsyncClient")
async def test_run_operations_monitor_with_disruption_and_reroute(mock_client_class):
    # Arrange
    # Mock httpx responses for alert creation and route reordering
    mock_client = AsyncMock()
    from unittest.mock import MagicMock
    mock_post_alert_response = AsyncMock()
    mock_post_alert_response.status_code = 201
    mock_post_alert_response.json = MagicMock(return_value={"id": "test-alert-id"})
    
    mock_post_reorder_response = AsyncMock()
    mock_post_reorder_response.status_code = 200
    mock_post_reorder_response.json = MagicMock(return_value={"optimizedOrder": [{"name": "Stop B"}, {"name": "Stop A"}, {"name": "Current"}]})

    # Setup the async context manager (__aenter__/__aexit__) and post responses
    mock_client.__aenter__.return_value = mock_client
    mock_client.post.side_effect = [mock_post_alert_response, mock_post_reorder_response]
    mock_client_class.return_value = mock_client

    req = MonitorOperationsRequest(
        booking_schedule_id=str(uuid.uuid4()),
        lat=6.0, # Above 5.0 so dummy disruption triggers
        lon=6.0
    )

    # Act
    with patch("app.agents.operations_agent.os.getenv", return_value=""): # Ensure no actual API keys trigger
        res = await run_operations_monitor(req)

    # Assert
    assert res.status == "DISRUPTION_DETECTED"
    assert "Disruption Alert:" in res.message
    assert res.rerouted is True
    assert res.disruption_alert_id == "test-alert-id"
    assert res.optimized_route == [{"name": "Stop B"}, {"name": "Stop A"}, {"name": "Current"}]
    
@pytest.mark.asyncio
@patch("app.agents.operations_agent.httpx.AsyncClient")
async def test_run_operations_monitor_backend_failure_gracefully_handled(mock_client_class):
    # Arrange
    mock_client = AsyncMock()
    mock_client.__aenter__.return_value = mock_client
    # Simulate a network error when posting to backend
    mock_client.post.side_effect = httpx.RequestError("Network error")
    mock_client_class.return_value = mock_client

    req = MonitorOperationsRequest(
        booking_schedule_id=str(uuid.uuid4()),
        lat=7.0, 
        lon=7.0
    )

    # Act
    with patch("app.agents.operations_agent.os.getenv", return_value=""):
        res = await run_operations_monitor(req)

    # Assert
    # It should still detect disruption and return safely even if backend API calls failed
    assert res.status == "DISRUPTION_DETECTED"
    assert res.rerouted is False # Because route reorder failed
    assert res.disruption_alert_id is None # Because alert post failed
