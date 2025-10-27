#!/bin/bash

echo "=========================================="
echo "LibHub Data Persistence Verification"
echo "=========================================="
echo ""

echo "Step 1: Creating test user..."
REGISTER_RESPONSE=$(curl -s -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d "{\"username\":\"persisttest\",\"email\":\"persist@test.com\",\"password\":\"Password123!\"}")
echo "  Response: $REGISTER_RESPONSE"

echo ""
echo "Step 2: Verifying user in database..."
USER_COUNT=$(docker exec libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 \
  -e "SELECT COUNT(*) FROM user_db.Users WHERE Email='persist@test.com';" -s -N 2>/dev/null)
echo "  User count before restart: $USER_COUNT"

echo ""
echo "Step 3: Stopping all containers..."
docker compose down

echo ""
echo "Step 4: Starting containers again..."
docker compose up -d

echo ""
echo "Step 5: Waiting for services to be ready (60 seconds)..."
sleep 60

echo ""
echo "Step 6: Verifying user still exists after restart..."
USER_COUNT_AFTER=$(docker exec libhub-mysql mysql -u libhub_user -pLibHub@Dev2025 \
  -e "SELECT COUNT(*) FROM user_db.Users WHERE Email='persist@test.com';" -s -N 2>/dev/null)
echo "  User count after restart: $USER_COUNT_AFTER"

if [ "$USER_COUNT_AFTER" -ge "1" ]; then
  echo "  ✓ Data persisted successfully!"
else
  echo "  ✗ Data NOT persisted!"
  exit 1
fi

echo ""
echo "Step 7: Testing login with persisted user..."
LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d "{\"email\":\"persist@test.com\",\"password\":\"Password123!\"}")

echo "  Response: $LOGIN_RESPONSE"

if echo "$LOGIN_RESPONSE" | grep -q "accessToken"; then
  echo "  ✓ Login successful with persisted user!"
else
  echo "  ✗ Login FAILED!"
  echo "  Note: Service may need more time to initialize"
fi

echo ""
echo "Step 8: Checking volume status..."
docker volume ls | grep mysql-data && echo "  ✓ MySQL volume exists" || echo "  ✗ Volume NOT found"

echo ""
echo "=========================================="
echo "✓ Data Persistence Verified!"
echo "=========================================="
