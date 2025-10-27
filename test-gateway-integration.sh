#!/bin/bash

echo "=== LibHub Gateway Integration Test ==="
echo ""

GATEWAY_URL="http://localhost:5000"

echo "1. Testing Public Endpoint - Get Books"
curl -s -o /dev/null -w "Status: %{http_code}\n" ${GATEWAY_URL}/api/books
echo ""

echo "2. Testing Public Endpoint - Register User"
REGISTER_RESPONSE=$(curl -s -X POST ${GATEWAY_URL}/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"testuser","email":"testgateway@test.com","password":"Test123!"}')
echo "Response: ${REGISTER_RESPONSE}"
echo ""

echo "3. Testing Authentication - Login"
LOGIN_RESPONSE=$(curl -s -X POST ${GATEWAY_URL}/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"testgateway@test.com","password":"Test123!"}')
echo "Response: ${LOGIN_RESPONSE}"
echo ""

TOKEN=$(echo ${LOGIN_RESPONSE} | grep -o '"accessToken":"[^"]*' | cut -d'"' -f4)

if [ -z "$TOKEN" ]; then
  echo "ERROR: Failed to get token. Cannot continue with protected endpoint tests."
  exit 1
fi

echo "Token obtained: ${TOKEN:0:50}..."
echo ""

echo "4. Testing Protected Endpoint - Get User Profile"
curl -s -o /dev/null -w "Status: %{http_code}\n" ${GATEWAY_URL}/api/users/me \
  -H "Authorization: Bearer ${TOKEN}"
echo ""

echo "5. Testing Protected Endpoint - Borrow Book (Saga workflow)"
BORROW_RESPONSE=$(curl -s -X POST ${GATEWAY_URL}/api/loans \
  -H "Authorization: Bearer ${TOKEN}" \
  -H "Content-Type: application/json" \
  -d '{"bookId":1}')
echo "Response: ${BORROW_RESPONSE}"
echo ""

echo "6. Testing Protected Endpoint - Get User Loans"
LOANS_RESPONSE=$(curl -s ${GATEWAY_URL}/api/loans/user/1 \
  -H "Authorization: Bearer ${TOKEN}")
echo "Response: ${LOANS_RESPONSE}"
echo ""

echo "=== Integration Test Complete ==="
