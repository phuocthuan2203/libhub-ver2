@echo off
setlocal enabledelayedexpansion

echo ========================================
echo LibHub Gateway Integration Tests
echo ========================================
echo.

set GATEWAY_URL=http://localhost:5000

echo [1/4] Testing Gateway health...
curl -s %GATEWAY_URL%/health
echo.

echo.
echo [2/4] Testing UserService through Gateway...
curl -s %GATEWAY_URL%/api/users/health
echo.

echo.
echo [3/4] Testing CatalogService through Gateway...
curl -s %GATEWAY_URL%/api/catalog/health
echo.

echo.
echo [4/4] Testing LoanService through Gateway...
curl -s %GATEWAY_URL%/api/loans/health
echo.

echo.
echo ========================================
echo Gateway Integration Tests Completed!
echo ========================================
echo.

endlocal
