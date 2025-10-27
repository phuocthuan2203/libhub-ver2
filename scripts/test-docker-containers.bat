@echo off
setlocal enabledelayedexpansion

echo ========================================
echo LibHub Docker Container Tests
echo ========================================
echo.

set GATEWAY_URL=http://localhost:5000
set CONSUL_URL=http://localhost:8500
set MYSQL_CONTAINER=libhub-mysql
set MYSQL_USER=libhub_user
set MYSQL_PASSWORD=LibHub@Dev2025

echo [1/7] Checking if Docker is running...
docker info >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Docker is not running. Please start Docker Desktop.
    exit /b 1
)
echo [OK] Docker is running

echo.
echo [2/7] Checking container status...
docker compose ps
if errorlevel 1 (
    echo [ERROR] Failed to get container status
    exit /b 1
)
echo [OK] Containers listed

echo.
echo [3/7] Checking MySQL health...
docker exec %MYSQL_CONTAINER% mysqladmin ping -h localhost >nul 2>&1
if errorlevel 1 (
    echo [ERROR] MySQL is not healthy
    exit /b 1
)
echo [OK] MySQL is healthy

echo.
echo [4/7] Checking databases...
docker exec %MYSQL_CONTAINER% mysql -u %MYSQL_USER% -p%MYSQL_PASSWORD% -e "SHOW DATABASES LIKE '%%_db';" >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Cannot access databases
    exit /b 1
)
echo [OK] Databases accessible

echo.
echo [5/7] Testing Consul...
curl -s %CONSUL_URL%/v1/status/leader >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Consul is not responding
    exit /b 1
)
echo [OK] Consul is responding

echo.
echo [6/7] Testing Gateway health...
curl -s %GATEWAY_URL%/health >nul 2>&1
if errorlevel 1 (
    echo [WARNING] Gateway health endpoint not responding
) else (
    echo [OK] Gateway is responding
)

echo.
echo [7/7] Checking service registrations...
curl -s %CONSUL_URL%/v1/catalog/services >nul 2>&1
if errorlevel 1 (
    echo [ERROR] Cannot check service registrations
    exit /b 1
)
echo [OK] Service registrations accessible

echo.
echo ========================================
echo All tests completed!
echo ========================================
echo.
echo Access points:
echo - Frontend: http://localhost:8080
echo - Gateway: http://localhost:5000
echo - Consul UI: http://localhost:8500
echo.

endlocal
