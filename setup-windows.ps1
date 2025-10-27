Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LibHub - Windows Setup Script" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Checking prerequisites..." -ForegroundColor Yellow
Write-Host ""

try {
    $dockerVersion = docker --version 2>&1
    Write-Host "[OK] Docker is installed: $dockerVersion" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not installed or not in PATH" -ForegroundColor Red
    Write-Host "Please install Docker Desktop from: https://www.docker.com/products/docker-desktop" -ForegroundColor Yellow
    exit 1
}

try {
    $null = docker info 2>&1
    if ($LASTEXITCODE -ne 0) { throw }
    Write-Host "[OK] Docker is running" -ForegroundColor Green
} catch {
    Write-Host "[ERROR] Docker is not running" -ForegroundColor Red
    Write-Host "Please start Docker Desktop and try again" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Starting LibHub Services..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "This will:" -ForegroundColor White
Write-Host "  1. Build all Docker images" -ForegroundColor White
Write-Host "  2. Start all services (Consul, MySQL, Microservices, Gateway, Frontend)" -ForegroundColor White
Write-Host "  3. Wait for services to be ready" -ForegroundColor White
Write-Host "  4. Run health checks" -ForegroundColor White
Write-Host ""
Write-Host "This may take 3-5 minutes on first run..." -ForegroundColor Yellow
Write-Host ""

docker compose up -d --build

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[ERROR] Failed to start services" -ForegroundColor Red
    Write-Host "Check the error messages above" -ForegroundColor Yellow
    exit 1
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Waiting for services to initialize..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

Write-Host "Waiting 60 seconds for all services to be ready..." -ForegroundColor Yellow
Start-Sleep -Seconds 60

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Running Health Checks..." -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

& "$PSScriptRoot\scripts\test-docker-containers.ps1"

if ($LASTEXITCODE -ne 0) {
    Write-Host ""
    Write-Host "[WARNING] Some health checks failed" -ForegroundColor Yellow
    Write-Host "Services might still be starting up" -ForegroundColor Yellow
    Write-Host "Wait a bit longer and run: .\scripts\test-docker-containers.ps1" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Setup Complete!" -ForegroundColor Green
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Your LibHub instance is running at:" -ForegroundColor White
Write-Host ""
Write-Host "  Frontend:  http://localhost:8080" -ForegroundColor Cyan
Write-Host "  Gateway:   http://localhost:5000" -ForegroundColor Cyan
Write-Host "  Consul UI: http://localhost:8500" -ForegroundColor Cyan
Write-Host ""
Write-Host "Useful commands:" -ForegroundColor White
Write-Host "  View logs:       docker compose logs -f" -ForegroundColor Gray
Write-Host "  Stop services:   docker compose down" -ForegroundColor Gray
Write-Host "  Restart:         docker compose restart" -ForegroundColor Gray
Write-Host ""
Write-Host "For more commands, see DOCKER_QUICK_START.md" -ForegroundColor Yellow
Write-Host ""
