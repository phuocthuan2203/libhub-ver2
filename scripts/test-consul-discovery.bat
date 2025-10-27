@echo off
setlocal enabledelayedexpansion

echo ========================================
echo LibHub Consul Service Discovery Tests
echo ========================================
echo.

set CONSUL_URL=http://localhost:8500

echo [1/5] Testing Consul availability...
curl -s %CONSUL_URL%/v1/status/leader
if errorlevel 1 (
    echo [ERROR] Consul is not available
    exit /b 1
)
echo [OK] Consul is available

echo.
echo [2/5] Listing registered services...
curl -s %CONSUL_URL%/v1/catalog/services
if errorlevel 1 (
    echo [ERROR] Cannot list services
    exit /b 1
)
echo.

echo.
echo [3/5] Checking UserService registration...
curl -s %CONSUL_URL%/v1/health/service/userservice
echo.

echo.
echo [4/5] Checking CatalogService registration...
curl -s %CONSUL_URL%/v1/health/service/catalogservice
echo.

echo.
echo [5/5] Checking LoanService registration...
curl -s %CONSUL_URL%/v1/health/service/loanservice
echo.

echo.
echo ========================================
echo Consul Discovery Tests Completed!
echo ========================================
echo Visit http://localhost:8500 for Consul UI
echo.

endlocal
