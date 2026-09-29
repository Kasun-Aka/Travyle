Write-Host "========================================================" -ForegroundColor Cyan
Write-Host "Starting Travyle Smart Booking Agent (LangGraph + FastAPI)" -ForegroundColor Green
Write-Host "Listening on http://localhost:8000" -ForegroundColor Yellow
Write-Host "========================================================" -ForegroundColor Cyan
Set-Location $PSScriptRoot

# Load OpenWeatherMap API key from backend appsettings.Development.json
$appsettingsPath = Join-Path $PSScriptRoot "..\appsettings.Development.json"
if (Test-Path $appsettingsPath) {
    $config = Get-Content $appsettingsPath -Raw | ConvertFrom-Json
    if ($config.OpenWeatherMap.ApiKey) {
        $env:OPENWEATHERMAP_API_KEY = $config.OpenWeatherMap.ApiKey
        Write-Host "Loaded OpenWeatherMap API key from appsettings.Development.json" -ForegroundColor Green
    }
}

python -m uvicorn app.main:app --host 0.0.0.0 --port 8000 --reload
