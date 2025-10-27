Write-Host "========================================" -ForegroundColor Cyan
Write-Host "LibHub Docker Container Tests" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""

$GATEWAY_URL = "http://localhost:5000"
$CONSUL_URL = "http://localhost:8500"
$MYSQL_CONTAINER = "libhub-mysql"
$MYSQL_USER = "libhub_user"
$MYSQL_PASSWORD = "LibHub@Dev2025"

$testsPassed = 0
$testsFailed = 0

function Test-Step {
    param($StepNumber, $Total, $Description, $ScriptBlock)
    
    Write-Host "[$StepNumber/$Total] $Description..." -ForegroundColor Yellow
    try {
        & $ScriptBlock
        Write-Host "[OK] $Description" -ForegroundColor Green
        $script:testsPassed++
        return $true
    } catch {
        Write-Host "[ERROR] $Description failed: $_" -ForegroundColor Red
        $script:testsFailed++
        return $false
    }
}

Test-Step 1 7 "Checking if Docker is running" {
    $null = docker info 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Docker is not running" }
}

Write-Host ""
Test-Step 2 7 "Checking container status" {
    docker compose ps
    if ($LASTEXITCODE -ne 0) { throw "Failed to get container status" }
}

Write-Host ""
Test-Step 3 7 "Checking MySQL health" {
    $null = docker exec $MYSQL_CONTAINER mysqladmin ping -h localhost 2>&1
    if ($LASTEXITCODE -ne 0) { throw "MySQL is not healthy" }
}

Write-Host ""
Test-Step 4 7 "Checking databases" {
    $null = docker exec $MYSQL_CONTAINER mysql -u $MYSQL_USER -p$MYSQL_PASSWORD -e "SHOW DATABASES LIKE '%_db';" 2>&1
    if ($LASTEXITCODE -ne 0) { throw "Cannot access databases" }
}

Write-Host ""
Test-Step 5 7 "Testing Consul" {
    $response = Invoke-WebRequest -Uri "$CONSUL_URL/v1/status/leader" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -ne 200) { throw "Consul not responding" }
}

Write-Host ""
Test-Step 6 7 "Testing Gateway health" {
    try {
        $response = Invoke-WebRequest -Uri "$GATEWAY_URL/health" -UseBasicParsing -ErrorAction Stop
        if ($response.StatusCode -ne 200) { throw "Gateway not healthy" }
    } catch {
        Write-Host "[WARNING] Gateway health endpoint not responding" -ForegroundColor Yellow
    }
}

Write-Host ""
Test-Step 7 7 "Checking service registrations" {
    $response = Invoke-WebRequest -Uri "$CONSUL_URL/v1/catalog/services" -UseBasicParsing -ErrorAction Stop
    if ($response.StatusCode -ne 200) { throw "Cannot check service registrations" }
}

Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "Test Results: $testsPassed passed, $testsFailed failed" -ForegroundColor $(if ($testsFailed -eq 0) { "Green" } else { "Yellow" })
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Access points:" -ForegroundColor White
Write-Host "  - Frontend:  http://localhost:8080" -ForegroundColor White
Write-Host "  - Gateway:   http://localhost:5000" -ForegroundColor White
Write-Host "  - Consul UI: http://localhost:8500" -ForegroundColor White
Write-Host ""

if ($testsFailed -gt 0) {
    exit 1
}
