#!/bin/bash

echo "Initializing LibHub databases..."
echo ""

if ! docker ps | grep -q libhub-mysql; then
    echo "Error: MySQL container is not running"
    echo "Please start the containers first with: docker compose up -d"
    exit 1
fi

echo "Waiting for MySQL to be ready..."
sleep 5

echo "Step 1: Creating databases and granting permissions..."
docker exec libhub-mysql mysql -u root -pLibHub@2025 -e "
CREATE DATABASE IF NOT EXISTS user_db;
CREATE DATABASE IF NOT EXISTS catalog_db;
CREATE DATABASE IF NOT EXISTS loan_db;
GRANT ALL PRIVILEGES ON user_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON catalog_db.* TO 'libhub_user'@'%';
GRANT ALL PRIVILEGES ON loan_db.* TO 'libhub_user'@'%';
FLUSH PRIVILEGES;
"

if [ $? -ne 0 ]; then
    echo "✗ Failed to create databases"
    exit 1
fi

echo "✓ Databases created successfully"
echo ""
echo "Step 2: Waiting for services to create tables (60 seconds)..."
sleep 60

echo ""
echo "Step 3: Applying seed data..."
SCRIPT_DIR="$( cd "$( dirname "${BASH_SOURCE[0]}" )" && pwd )"
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 < "$SCRIPT_DIR/seed-data.sql"

if [ $? -eq 0 ]; then
    echo ""
    echo "✓ Seed data applied successfully!"
    echo ""
    echo "You can now log in with:"
    echo "  - Username: admin / Email: admin@libhub.com"
    echo "  - Username: testuser / Email: test@libhub.com"
else
    echo ""
    echo "✗ Failed to apply seed data"
    echo "Check if the services have created the tables first"
    echo "You may need to wait longer and try again"
fi
