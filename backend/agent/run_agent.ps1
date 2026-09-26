Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Starting Travyle Smart Booking Agent (LangGraph + FastAPI)" -ForegroundColor Green
Write-Host "Listening on http://localhost:8000" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Set-Location $PSScriptRoot
python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
