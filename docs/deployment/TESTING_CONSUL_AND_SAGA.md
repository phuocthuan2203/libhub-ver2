# Testing Consul Service Discovery & Saga Pattern

## Overview

This guide demonstrates:
1. **Service Registration** - How services register with Consul on startup
2. **Service Discovery** - How Gateway discovers services through Consul
3. **Load Balancing** - How requests are distributed across 3 instances
4. **Saga Pattern** - How distributed transactions work across services

## Current Setup

All microservices are scaled to **3 instances each**:
- **UserService**: 3 instances
- **CatalogService**: 3 instances  
- **LoanService**: 3 instances

Total: **9 microservice instances** + Gateway + Frontend + Consul + MySQL = **13 containers**

---

## Part 1: Observing Service Registration to Consul

### Step 1: View Service Registration Logs

Watch how each service registers itself with Consul on startup.

```bash
# UserService instances registering
docker logs libhub-userservice-1 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-userservice-2 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-userservice-3 2>&1 | grep -i "consul\|register\|health"
```

**What to look for:**
- Service registration with unique ID (GUID)
- Health check endpoint configured
- Consul connection established

### Step 2: View All Service Registrations

```bash
# CatalogService instances
docker logs libhub-catalogservice-1 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-catalogservice-2 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-catalogservice-3 2>&1 | grep -i "consul\|register\|health"

# LoanService instances
docker logs libhub-loanservice-1 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-loanservice-2 2>&1 | grep -i "consul\|register\|health"
docker logs libhub-loanservice-3 2>&1 | grep -i "consul\|register\|health"
```

### Step 3: Verify in Consul UI

Open Consul UI: **http://localhost:8500**

Navigate to **Services** tab. You should see:
- `userservice` - 3 instances (all passing health checks)
- `catalogservice` - 3 instances (all passing health checks)
- `loanservice` - 3 instances (all passing health checks)

Click on each service to see:
- Service ID (unique GUID for each instance)
- IP Address and Port
- Health check status
- Last health check time

---

## Part 2: Observing Gateway → Consul → Service Flow

### Step 1: Enable Gateway Logging

Open a new terminal and follow Gateway logs in real-time:

```bash
docker logs -f libhub-gateway
```

### Step 2: Make a Request Through Gateway

In another terminal, make a request:

```bash
curl -s http://localhost:5000/api/books | jq '.[0:2]'
```

**What happens:**
1. Gateway receives request at `/api/books`
2. Gateway queries Consul for `catalogservice` instances
3. Consul returns list of healthy instances
4. Gateway selects one instance (RoundRobin)
5. Gateway forwards request to selected instance
6. Response returned to client

### Step 3: Observe Load Balancing

Make multiple requests and watch which instance handles each:

```bash
# Terminal 1: Watch CatalogService-1 logs
docker logs -f libhub-catalogservice-1

# Terminal 2: Watch CatalogService-2 logs
docker logs -f libhub-catalogservice-2

# Terminal 3: Watch CatalogService-3 logs
docker logs -f libhub-catalogservice-3

# Terminal 4: Make multiple requests
for i in {1..9}; do
  echo "Request $i:"
  curl -s http://localhost:5000/api/books | jq 'length'
  sleep 1
done
```

**Expected behavior:**
- Requests distributed across all 3 instances
- RoundRobin pattern: Instance-1 → Instance-2 → Instance-3 → Instance-1...
- Each instance logs the incoming request

### Step 4: Query Consul for Service Instances

See what Gateway sees when querying Consul:

```bash
# Get all catalogservice instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq '.[] | {
  ServiceID: .Service.ID,
  Address: .Service.Address,
  Port: .Service.Port,
  Status: .Checks[1].Status
}'
```

---

## Part 3: Observing the Saga Pattern

The Saga pattern coordinates distributed transactions across LoanService and CatalogService.

### Saga Flow for Borrowing a Book

```
1. LoanService: Check user loan limit (max 5 active loans)
2. LoanService: Create PENDING loan in loan_db
3. LoanService → Consul: Query for catalogservice instances
4. Consul → LoanService: Return healthy catalogservice instances
5. LoanService → CatalogService: GET /api/books/{id} (verify availability)
6. CatalogService: Check if book has available copies
7. LoanService → CatalogService: PUT /api/books/{id}/decrement-stock
8. CatalogService: Decrement AvailableCopies in catalog_db
9. LoanService: Update loan status to CheckedOut
10. Success! Distributed transaction complete.

If any step fails → Compensating transaction (mark loan as FAILED)
```

### Step 1: Prepare for Saga Testing

First, register a user and login to get JWT token:

```bash
# Register a test user
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "testuser",
    "email": "testuser@test.com",
    "password": "Test@1234"
  }'

# Login to get JWT token
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "testuser@test.com",
    "password": "Test@1234"
  }' | jq -r '.token')

echo "Token: $TOKEN"
```

### Step 2: Monitor All Services for Saga

Open 4 terminals to watch the Saga in action:

```bash
# Terminal 1: Watch LoanService logs (Saga orchestrator)
docker logs -f libhub-loanservice-1 2>&1 | grep -i "saga\|borrow\|loan\|stock"

# Terminal 2: Watch CatalogService logs (Saga participant)
docker logs -f libhub-catalogservice-1 2>&1 | grep -i "stock\|decrement\|book"

# Terminal 3: Watch Gateway logs (routing)
docker logs -f libhub-gateway 2>&1 | grep -i "loan\|catalog"

# Terminal 4: Watch Consul health checks
watch -n 2 'curl -s http://localhost:8500/v1/health/service/loanservice?passing | jq "length"'
```

### Step 3: Execute Saga - Borrow a Book

In a new terminal, borrow a book (triggers Saga):

```bash
# Borrow book ID 1
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "userId": 1,
    "bookId": 1
  }' | jq '.'
```

### Step 4: Observe Saga Execution

**In LoanService logs, you should see:**
```
[Saga Step 1] Checking user loan limit...
[Saga Step 2] Creating PENDING loan in database...
[Saga Step 3] Querying Consul for catalogservice...
[Saga Step 4] Verifying book availability...
[Saga Step 5] Calling CatalogService to decrement stock...
[Saga Success] Loan marked as CheckedOut
```

**In CatalogService logs, you should see:**
```
GET /api/books/1 - Book availability check
PUT /api/books/1/decrement-stock - Stock decremented
AvailableCopies: 5 → 4
```

**In Gateway logs, you should see:**
```
Routing POST /api/loans → loanservice (instance X)
Routing GET /api/books/1 → catalogservice (instance Y)
Routing PUT /api/books/1/decrement-stock → catalogservice (instance Z)
```

### Step 5: Verify Saga Results

Check the loan was created:

```bash
# Get user's loans
curl -s http://localhost:5000/api/loans/user/1 \
  -H "Authorization: Bearer $TOKEN" | jq '.'
```

Check the book stock was decremented:

```bash
# Get book details
curl -s http://localhost:5000/api/books/1 | jq '{
  title: .title,
  totalCopies: .totalCopies,
  availableCopies: .availableCopies
}'
```

### Step 6: Test Saga Compensating Transaction (Failure Scenario)

Try to borrow a book that doesn't exist (triggers compensation):

```bash
# Try to borrow non-existent book
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{
    "userId": 1,
    "bookId": 999
  }'
```

**In LoanService logs, you should see:**
```
[Saga Step 1] Checking user loan limit... ✓
[Saga Step 2] Creating PENDING loan... ✓
[Saga Step 3] Verifying book availability... ✗ Book not found
[Saga Compensation] Marking loan as FAILED
[Saga Rollback] No stock was decremented, no compensation needed
```

---

## Part 4: Testing Load Balancing Across Instances

### Test 1: Round-Robin Distribution

```bash
# Make 12 requests and see distribution
for i in {1..12}; do
  echo "Request $i"
  curl -s http://localhost:5000/api/books/1 | jq '.bookId'
done

# Check which instances handled requests
echo "=== CatalogService-1 ==="
docker logs libhub-catalogservice-1 2>&1 | grep "GET /api/books/1" | wc -l

echo "=== CatalogService-2 ==="
docker logs libhub-catalogservice-2 2>&1 | grep "GET /api/books/1" | wc -l

echo "=== CatalogService-3 ==="
docker logs libhub-catalogservice-3 2>&1 | grep "GET /api/books/1" | wc -l
```

**Expected:** Each instance handles ~4 requests (12 ÷ 3 = 4)

### Test 2: Simulate Instance Failure

```bash
# Stop one instance
docker stop libhub-catalogservice-2

# Wait for health check to fail (10 seconds)
sleep 15

# Check Consul - should show only 2 healthy instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'

# Make requests - should only go to 2 instances
for i in {1..6}; do
  curl -s http://localhost:5000/api/books | jq 'length'
done

# Restart instance
docker start libhub-catalogservice-2

# Wait for re-registration
sleep 15

# Verify 3 instances again
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'
```

---

## Part 5: Complete End-to-End Saga Test

### Full User Journey with Saga

```bash
# 1. Register user
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "sagatest",
    "email": "saga@test.com",
    "password": "Test@1234"
  }' | jq '.'

# 2. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{
    "email": "saga@test.com",
    "password": "Test@1234"
  }' | jq -r '.token')

# 3. Browse books (through Consul discovery)
curl -s http://localhost:5000/api/books | jq '.[0:3] | .[] | {id: .bookId, title: .title, available: .availableCopies}'

# 4. Check specific book
BOOK_ID=2
curl -s http://localhost:5000/api/books/$BOOK_ID | jq '{title, availableCopies}'

# 5. Borrow book (Saga execution)
echo "=== BORROWING BOOK (SAGA STARTS) ==="
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d "{
    \"userId\": 2,
    \"bookId\": $BOOK_ID
  }" | jq '.'

# 6. Verify loan created
curl -s http://localhost:5000/api/loans/user/2 \
  -H "Authorization: Bearer $TOKEN" | jq '.[] | {loanId, bookId, status}'

# 7. Verify stock decremented
curl -s http://localhost:5000/api/books/$BOOK_ID | jq '{title, availableCopies}'

# 8. Return book (Saga compensation)
LOAN_ID=$(curl -s http://localhost:5000/api/loans/user/2 \
  -H "Authorization: Bearer $TOKEN" | jq -r '.[0].loanId')

echo "=== RETURNING BOOK (SAGA COMPENSATION) ==="
curl -X PUT http://localhost:5000/api/loans/$LOAN_ID/return \
  -H "Authorization: Bearer $TOKEN" | jq '.'

# 9. Verify stock incremented
curl -s http://localhost:5000/api/books/$BOOK_ID | jq '{title, availableCopies}'
```

---

## Part 6: Monitoring Commands

### Real-time Monitoring

```bash
# Watch all service registrations in Consul
watch -n 2 'curl -s http://localhost:8500/v1/catalog/services | jq .'

# Monitor health checks
watch -n 2 'curl -s http://localhost:8500/v1/health/state/passing | jq "length"'

# Watch container resource usage
docker stats

# Monitor specific service logs
docker logs -f libhub-loanservice-1 --tail 50

# Watch Gateway routing decisions
docker logs -f libhub-gateway --tail 50
```

### Consul Queries

```bash
# Get all service instances with health status
curl -s http://localhost:8500/v1/health/service/catalogservice | jq '.[] | {
  Node: .Node.Node,
  ServiceID: .Service.ID,
  Address: .Service.Address,
  Port: .Service.Port,
  Status: .Checks[1].Status,
  Output: .Checks[1].Output
}'

# Get only passing instances
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'

# Get service catalog
curl -s http://localhost:8500/v1/catalog/service/loanservice | jq '.[] | {
  ServiceID: .ServiceID,
  ServiceAddress: .ServiceAddress,
  ServicePort: .ServicePort
}'
```

---

## Summary of What You'll Observe

### 1. Service Registration
- Each of 9 instances registers with Consul on startup
- Unique service ID (GUID) for each instance
- Health check endpoint configured at `/health`
- Consul performs health checks every 10 seconds

### 2. Service Discovery
- Gateway queries Consul for service instances
- Consul returns list of healthy instances
- Gateway uses RoundRobin load balancing
- Requests distributed evenly across instances

### 3. Load Balancing
- 3 instances per service
- Requests distributed: 1 → 2 → 3 → 1 → 2 → 3...
- Failed instances automatically removed from rotation
- Recovered instances automatically added back

### 4. Saga Pattern
- **Step 1**: Check loan limit (LoanService)
- **Step 2**: Create PENDING loan (LoanService → loan_db)
- **Step 3**: Verify book availability (LoanService → Consul → CatalogService)
- **Step 4**: Decrement stock (LoanService → CatalogService → catalog_db)
- **Step 5**: Mark loan CheckedOut (LoanService → loan_db)
- **Compensation**: If any step fails, mark loan as FAILED

### 5. Distributed Transaction
- Coordinates across 2 databases (loan_db + catalog_db)
- Coordinates across 2 microservices (LoanService + CatalogService)
- Uses Consul for service discovery
- Implements compensating transactions for failures

---

## Quick Test Script

Save this as `test-all.sh` and run it:

```bash
#!/bin/bash

echo "=== Testing Consul Service Discovery & Saga Pattern ==="
echo ""

echo "1. Checking Consul services..."
curl -s http://localhost:8500/v1/catalog/services | jq .
echo ""

echo "2. Checking service instances..."
for service in userservice catalogservice loanservice; do
  count=$(curl -s http://localhost:8500/v1/health/service/$service?passing | jq 'length')
  echo "  $service: $count healthy instances"
done
echo ""

echo "3. Testing load balancing (9 requests)..."
for i in {1..9}; do
  curl -s http://localhost:5000/api/books/1 > /dev/null
  echo "  Request $i sent"
done
echo ""

echo "4. Checking request distribution..."
for i in 1 2 3; do
  count=$(docker logs libhub-catalogservice-$i 2>&1 | grep -c "GET /api/books/1")
  echo "  CatalogService-$i: $count requests"
done
echo ""

echo "5. Testing Saga pattern..."
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"testuser@test.com","password":"Test@1234"}' | jq -r '.token')

if [ "$TOKEN" != "null" ]; then
  echo "  Borrowing book (Saga execution)..."
  curl -s -X POST http://localhost:5000/api/loans \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d '{"userId":1,"bookId":3}' | jq '.status'
else
  echo "  Please register a user first"
fi

echo ""
echo "=== Test Complete ==="
```

---

## Troubleshooting

### Services not registering with Consul
```bash
# Check Consul logs
docker logs libhub-consul

# Check service logs for registration errors
docker logs libhub-userservice-1 2>&1 | grep -i error
```

### Load balancing not working
```bash
# Verify all instances are healthy
curl -s http://localhost:8500/v1/health/service/catalogservice?passing | jq 'length'

# Check Gateway Consul configuration
docker logs libhub-gateway 2>&1 | grep -i consul
```

### Saga failing
```bash
# Check LoanService logs
docker logs libhub-loanservice-1 2>&1 | grep -i saga

# Check CatalogService connectivity
curl -s http://localhost:5001/api/books/1 | jq .
```

---

**Happy Testing! 🎉**
