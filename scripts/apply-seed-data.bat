@echo off
echo Initializing LibHub databases...
echo.

docker ps | findstr libhub-mysql >nul 2>&1
if errorlevel 1 (
    echo Error: MySQL container is not running
    echo Please start the containers first with: docker compose -f docker-compose.windows.yml up -d
    exit /b 1
)

echo Waiting for MySQL to be ready...
timeout /t 5 /nobreak >nul

echo Step 1: Creating databases and granting permissions...
docker exec libhub-mysql mysql -u root -pLibHub@2025 -e "CREATE DATABASE IF NOT EXISTS user_db; CREATE DATABASE IF NOT EXISTS catalog_db; CREATE DATABASE IF NOT EXISTS loan_db; GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%%'; GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%%'; GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%%'; FLUSH PRIVILEGES;"

if errorlevel 1 (
    echo.
    echo [X] Failed to create databases
    exit /b 1
)

echo [OK] Databases created successfully
echo.
echo Step 2: Waiting for services to create tables (60 seconds)...
timeout /t 60 /nobreak >nul

echo.
echo Step 3: Applying seed data...
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 < scripts\seed-data.sql

if errorlevel 1 (
    echo.
    echo [X] Failed to apply seed data
    echo Check if the services have created the tables first
    echo You may need to wait longer and try again
    exit /b 1
)

echo.
echo [OK] Seed data applied successfully!
echo.
echo You can now log in with:
echo   - Username: admin / Email: admin@libhub.com
echo   - Username: testuser / Email: test@libhub.com
