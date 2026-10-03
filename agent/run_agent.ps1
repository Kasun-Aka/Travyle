Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Starting Travyle Smart Booking Agent (LangGraph + FastAPI)" -ForegroundColor Green
Write-Host "Listening on http://localhost:8000" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Set-Location $PSScriptRoot
# Environment variables are loaded automatically from .env via python-dotenv


python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
