#!/bin/bash

echo "Applying seed data to LibHub databases..."
echo ""

if ! docker ps | grep -q libhub-mysql; then
    echo "Error: MySQL container is not running"
    echo "Please start the containers first with: docker compose up -d"
    exit 1
fi

echo "Waiting for MySQL to be ready..."
sleep 5

echo "Applying seed data..."
docker exec -i libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 < /home/thuannp4/development/LibHub/scripts/seed-data.sql

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
    echo "Wait a minute and try again"
fi
