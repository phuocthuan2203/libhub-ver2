#!/bin/bash

echo "==================================="
echo "Consul Service Discovery Test"
echo "==================================="
echo ""

echo "1. Checking Consul availability..."
if curl -s http://localhost:8500/v1/status/leader > /dev/null; then
    echo "✓ Consul is running"
else
    echo "✗ Consul is not accessible"
    exit 1
fi
echo ""

echo "2. Listing registered services..."
SERVICES=$(curl -s http://localhost:8500/v1/catalog/services | jq -r 'keys[]')
echo "$SERVICES"
echo ""

echo "3. Checking service health..."
for service in userservice catalogservice loanservice; do
    HEALTH=$(curl -s "http://localhost:8500/v1/health/service/$service?passing" | jq -r 'length')
    if [ "$HEALTH" -gt 0 ]; then
        echo "✓ $service: $HEALTH healthy instance(s)"
    else
        echo "✗ $service: No healthy instances"
    fi
done
echo ""

echo "4. Testing service endpoints..."
echo "Testing UserService health..."
curl -s http://localhost:5002/health && echo "✓ UserService health OK" || echo "✗ UserService health failed"

echo "Testing CatalogService health..."
curl -s http://localhost:5001/health && echo "✓ CatalogService health OK" || echo "✗ CatalogService health failed"

echo "Testing LoanService health..."
curl -s http://localhost:5003/health && echo "✓ LoanService health OK" || echo "✗ LoanService health failed"
echo ""

echo "5. Testing Gateway routing through Consul..."
echo "GET /api/books through Gateway..."
RESPONSE=$(curl -s -o /dev/null -w "%{http_code}" http://localhost:5000/api/books)
if [ "$RESPONSE" = "200" ] || [ "$RESPONSE" = "401" ]; then
    echo "✓ Gateway routing works (HTTP $RESPONSE)"
else
    echo "✗ Gateway routing failed (HTTP $RESPONSE)"
fi
echo ""

echo "6. Service instance details..."
for service in userservice catalogservice loanservice; do
    echo "--- $service ---"
    curl -s "http://localhost:8500/v1/health/service/$service" | jq -r '.[] | "\(.Node.Node): \(.Service.Address):\(.Service.Port) - \(.Checks[1].Status)"'
done
echo ""

echo "==================================="
echo "Test completed!"
echo "==================================="
echo ""
echo "Consul UI: http://localhost:8500"
echo "Gateway: http://localhost:5000"
