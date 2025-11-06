# Phase 3 Testing Quick Reference

## Quick Start Test Commands

### 1. Start All Services
```bash
cd /home/thuannp4/development/libhub-ver2
docker compose up -d
```

### 2. Monitor Logs (4 Terminals)
```bash
# Terminal 1: Gateway
docker logs -f libhub-gateway

# Terminal 2: LoanService  
docker logs -f libhub-ver2-loanservice-1

# Terminal 3: CatalogService
docker logs -f libhub-ver2-catalogservice-1

# Terminal 4: UserService
docker logs -f libhub-ver2-userservice-1
```

---

## Test Scenarios

### Test 1: Consul Registration
**What to check:** Service startup logs for `🔌 [CONSUL-REGISTER]` and `✅ [CONSUL-SUCCESS]`

```bash
# Restart a service to see registration
docker compose restart userservice
docker logs libhub-ver2-userservice-1 2>&1 | grep CONSUL
```

**Expected:**
```
🔌 [CONSUL-REGISTER] Service: userservice | ID: ... | Address: userservice:5002 | Health: http://userservice:5002/health | Attempt: 1/5
✅ [CONSUL-SUCCESS] Service: userservice | ID: ... | Address: userservice:5002 registered successfully
```

---

### Test 2: Complete Borrow Flow

```bash
# 1. Login
LOGIN_RESP=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john.doe@example.com","password":"password123"}')

TOKEN=$(echo $LOGIN_RESP | jq -r '.token')

# 2. Borrow a book
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId":1}'
```

**Look for in logs:**
- Gateway: `✅ [JWT-SUCCESS]`
- LoanService: `🚀 [SAGA-START]` → `📝 [SAGA-STEP-1]` → `🔍 [SAGA-STEP-2]` → `📉 [SAGA-STEP-3]` → `🎉 [SAGA-SUCCESS]`
- CatalogService: `📦 [STOCK-UPDATE-START]` → `✅ [STOCK-UPDATE-SUCCESS]`

---

### Test 3: Failed JWT Validation

```bash
# Use invalid token
curl -X GET http://localhost:5000/api/books \
  -H "Authorization: Bearer invalid_token"
```

**Look for in Gateway logs:**
```
❌ [JWT-FAILED] Authentication failed | Reason: ...
```

---

### Test 4: Book Return

```bash
# Get loan ID
LOANS=$(curl -s -X GET http://localhost:5000/api/loans \
  -H "Authorization: Bearer $TOKEN")

LOAN_ID=$(echo $LOANS | jq -r '.[0].loanId')

# Return the book
curl -X PUT http://localhost:5000/api/loans/$LOAN_ID/return \
  -H "Authorization: Bearer $TOKEN"
```

**Look for in LoanService logs:**
```
📚 [RETURN-START] Processing book return | LoanId: ...
📈 [RETURN-STEP-2] Incrementing book stock | BookId: ...
🎉 [RETURN-SUCCESS] Return completed successfully | LoanId: ... | BookId: ...
```

---

### Test 5: User Registration & Login

```bash
# Register
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "email":"newuser@example.com",
    "password":"password123",
    "firstName":"New",
    "lastName":"User"
  }'

# Login
curl -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"newuser@example.com","password":"password123"}'
```

**Look for in UserService logs:**
```
📝 [REGISTER-ATTEMPT] Registration attempt | Email: newuser@example.com
✅ [REGISTER-SUCCESS] User registered successfully | Email: newuser@example.com | UserId: ...
🔐 [LOGIN-ATTEMPT] Login attempt | Email: newuser@example.com
✅ [LOGIN-SUCCESS] User logged in successfully | Email: newuser@example.com
```

---

## Search Logs by Pattern

### Find all Consul events
```bash
docker logs libhub-ver2-userservice-1 2>&1 | grep CONSUL
```

### Find all Saga events
```bash
docker logs libhub-ver2-loanservice-1 2>&1 | grep SAGA
```

### Find all JWT events
```bash
docker logs libhub-gateway 2>&1 | grep JWT
```

### Find all failures
```bash
docker logs libhub-ver2-loanservice-1 2>&1 | grep -E "FAILED|💥"
```

### Find by Correlation ID
```bash
docker logs libhub-ver2-loanservice-1 2>&1 | grep "req-xyz"
```

---

## Emoji Quick Reference

| What to Find | Emoji | Tag |
|--------------|-------|-----|
| Consul ops | 🔌 | CONSUL-REGISTER |
| Success | ✅ | *-SUCCESS |
| Complete | 🎉 | SAGA-SUCCESS, RETURN-SUCCESS |
| Failures | ❌ 💥 | *-FAILED |
| Warnings | ⚠️ | CONSUL-RETRY, JWT-CHALLENGE |
| Saga start | 🚀 | SAGA-START |
| Checking | 🔍 | SAGA-STEP-2 |
| Stock ops | 📦 📉 📈 | STOCK-UPDATE, decrement, increment |
| Returns | 📚 | RETURN-START |
| HTTP calls | 🔗 📨 | HTTP-CALL, HTTP-RESPONSE |
| Auth | 🔐 📝 | LOGIN, REGISTER |
| Health | 💓 | HEALTH-CHECK |

---

## Troubleshooting

### No logs appearing?
```bash
# Check if services are running
docker ps

# Check specific service logs
docker logs libhub-ver2-userservice-1
```

### Emojis not displaying?
They should work in most terminals. Tags like `[SAGA-START]` will still be visible.

### Health check logs not visible?
They use DEBUG level. Either:
1. Check Consul UI: http://localhost:8500 to verify health checks work
2. Call endpoint manually: `curl http://localhost:5002/health`

---

## Full End-to-End Test Script

Save this as `test-phase3.sh`:

```bash
#!/bin/bash

echo "🧪 Testing Phase 3 Enhanced Logging"
echo "=================================="

# Login
echo "1. 🔐 Logging in..."
LOGIN_RESP=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"email":"john.doe@example.com","password":"password123"}')

TOKEN=$(echo $LOGIN_RESP | jq -r '.token')
echo "   Token: ${TOKEN:0:20}..."

# Borrow
echo "2. 📚 Borrowing book..."
BORROW_RESP=$(curl -s -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId":1}')

LOAN_ID=$(echo $BORROW_RESP | jq -r '.loanId')
echo "   Loan ID: $LOAN_ID"

# Return
echo "3. ↩️  Returning book..."
curl -s -X PUT http://localhost:5000/api/loans/$LOAN_ID/return \
  -H "Authorization: Bearer $TOKEN" > /dev/null

echo ""
echo "✅ Test completed! Check logs for enhanced logging:"
echo "   docker logs libhub-ver2-loanservice-1 2>&1 | grep SAGA"
```

Run with:
```bash
chmod +x test-phase3.sh
./test-phase3.sh
```
