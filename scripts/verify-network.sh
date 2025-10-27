#!/bin/bash

echo "=========================================="
echo "LibHub Network Verification Script"
echo "=========================================="
echo ""

echo "Step 1: Inspecting libhub-network..."
docker network inspect libhub_libhub-network

echo ""
echo "Step 2: Listing all containers and their networks..."
docker ps -a --format "table {{.Names}}\t{{.Networks}}\t{{.Status}}"

echo ""
echo "Step 3: Testing inter-service communication..."

echo "  3.1: LoanService -> CatalogService (DNS resolution)..."
docker exec libhub-loanservice getent hosts catalogservice > /dev/null 2>&1 && echo "    ✓ DNS resolution successful" || echo "    ✗ DNS resolution FAILED"

echo ""
echo "  3.2: Gateway -> UserService (DNS resolution)..."
docker exec libhub-gateway getent hosts userservice > /dev/null 2>&1 && echo "    ✓ DNS resolution successful" || echo "    ✗ DNS resolution FAILED"

echo ""
echo "  3.3: Gateway -> CatalogService (DNS resolution)..."
docker exec libhub-gateway getent hosts catalogservice > /dev/null 2>&1 && echo "    ✓ DNS resolution successful" || echo "    ✗ DNS resolution FAILED"

echo ""
echo "  3.4: Gateway -> LoanService (DNS resolution)..."
docker exec libhub-gateway getent hosts loanservice > /dev/null 2>&1 && echo "    ✓ DNS resolution successful" || echo "    ✗ DNS resolution FAILED"

echo ""
echo "Step 4: Testing MySQL connectivity from services..."

echo "  4.1: UserService -> MySQL..."
docker exec libhub-userservice bash -c "apt-get update -qq && apt-get install -y -qq mysql-client > /dev/null 2>&1 && mysql -h mysql -u libhub_user -pLibHub@Dev2025 -e 'SELECT 1;' > /dev/null 2>&1" && echo "    ✓ MySQL connection successful" || echo "    ✗ MySQL connection FAILED"

echo ""
echo "Step 5: DNS resolution tests..."

echo "  5.1: All services -> MySQL..."
docker exec libhub-userservice getent hosts mysql > /dev/null 2>&1 && echo "    ✓ MySQL DNS resolution successful" || echo "    ✗ MySQL DNS resolution FAILED"

echo ""
echo "=========================================="
echo "✓ Network Verification Complete!"
echo "=========================================="
