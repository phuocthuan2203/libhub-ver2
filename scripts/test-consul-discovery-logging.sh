#!/bin/bash

echo "=========================================="
echo "Testing Consul Service Discovery Logging"
echo "=========================================="
echo ""

echo "Step 1: Starting Docker containers..."
cd /home/thuannp4/development/libhub-ver2
docker compose up -d

echo ""
echo "Step 2: Waiting for services to be healthy (30 seconds)..."
sleep 30

echo ""
echo "Step 3: Check if services are registered in Consul..."
curl -s http://localhost:8500/v1/agent/services | jq .

echo ""
echo "Step 4: Make a test request to the Gateway..."
echo "Request: GET /api/books"
echo ""

CORRELATION_ID="test-$(date +%s)-$(openssl rand -hex 4)"
echo "Correlation ID: $CORRELATION_ID"
echo ""

curl -v -H "X-Correlation-ID: $CORRELATION_ID" http://localhost:5000/api/books 2>&1 | grep -E "(< |> |X-Correlation-ID)"

echo ""
echo ""
echo "Step 5: Check Gateway logs for Consul discovery..."
echo "Looking for logs with pattern: [CONSUL-QUERY], [CONSUL-RESPONSE], [CONSUL-RESOLVED]"
echo ""

docker logs libhub-gateway 2>&1 | grep -E "\[CONSUL-QUERY\]|\[CONSUL-RESPONSE\]|\[CONSUL-RESOLVED\]" | tail -20

echo ""
echo ""
echo "Step 6: Make a loan request (requires authentication)..."
echo "First, register a user and login..."
echo ""

REGISTER_RESPONSE=$(curl -s -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: $CORRELATION_ID-register" \
  -d '{
    "email": "testuser@example.com",
    "password": "Test123!",
    "fullName": "Test User"
  }')

echo "Registration response: $REGISTER_RESPONSE"
echo ""

LOGIN_RESPONSE=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -H "X-Correlation-ID: $CORRELATION_ID-login" \
  -d '{
    "email": "testuser@example.com",
    "password": "Test123!"
  }')

TOKEN=$(echo $LOGIN_RESPONSE | jq -r '.token')
echo "Login successful, got token: ${TOKEN:0:20}..."
echo ""

echo "Step 7: Borrow a book (triggers LoanService -> CatalogService call)..."
echo ""

BORROW_RESPONSE=$(curl -s -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -H "X-Correlation-ID: $CORRELATION_ID-borrow" \
  -d '{
    "bookId": 1
  }')

echo "Borrow response: $BORROW_RESPONSE"
echo ""

echo ""
echo "Step 8: Check all service logs for the correlation ID..."
echo "Correlation ID: $CORRELATION_ID-borrow"
echo ""

echo "=== Gateway Logs ==="
docker logs libhub-gateway 2>&1 | grep "$CORRELATION_ID-borrow" | tail -10

echo ""
echo "=== LoanService Logs ==="
docker logs libhub-loanservice 2>&1 | grep "$CORRELATION_ID-borrow" | tail -10

echo ""
echo "=== CatalogService Logs ==="
docker logs libhub-catalogservice 2>&1 | grep "$CORRELATION_ID-borrow" | tail -10

echo ""
echo ""
echo "Step 9: Open Seq UI to view structured logs..."
echo "URL: http://localhost:5341"
echo "Search query: CorrelationId = '$CORRELATION_ID-borrow'"
echo ""

echo "=========================================="
echo "Test completed!"
echo "=========================================="
