# Phase 5: Consul Service Discovery - Testing Guide

**Date:** November 9, 2025  
**Feature:** Dynamic service discovery for LoanService → CatalogService communication

---

## Quick Test (5 Minutes)

### Prerequisites
```bash
# Ensure all services are running
docker compose up -d --build

# Wait 30 seconds for services to register with Consul
sleep 30
```

### Test 1: Verify Consul Registration

**Check Consul UI:**
```
http://localhost:8500
```

✅ **Expected:** All services show as healthy (green)
- userservice
- catalogservice  
- loanservice

**Or via command line:**
```bash
curl http://localhost:8500/v1/catalog/services | jq
```

---

### Test 2: Trigger Service Discovery

**Complete workflow script:**

```bash
# 1. Register user
curl -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{
    "username": "sdtest",
    "email": "sdtest@test.com",
    "password": "Test123!"
  }'

# 2. Login
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sdtest","password":"Test123!"}' \
  | grep -o '"token":"[^"]*' | sed 's/"token":"//')

echo "Token: $TOKEN"

# 3. Borrow a book (THIS TRIGGERS SERVICE DISCOVERY!)
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId": 1}'
```

---

### Test 3: View Logs

**Option A: Docker Logs**
```bash
docker logs libhub-loanservice-1 --tail 30 | grep -E "SERVICE-DISCOVERY|INTER-SERVICE"
```

**Expected output:**
```
🔍 [SERVICE-DISCOVERY] Querying Consul for service: catalogservice
✅ [SERVICE-DISCOVERY] Discovered service: catalogservice at http://catalogservice:5001 | ServiceId: catalogservice-xxx | HealthStatus: Passing
🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: GET /api/books/1
📨 [INTER-SERVICE] CatalogService response: 200 for GET /api/books/1
🔗 [INTER-SERVICE] Calling CatalogService at http://catalogservice:5001: PUT /api/books/1/stock (decrement)
📨 [INTER-SERVICE] CatalogService response: 200 for PUT /api/books/1/stock
```

**Option B: Seq Dashboard (Recommended)**
1. Open http://localhost:5341
2. Filter: `ServiceName = 'LoanService' and (@Message like '%SERVICE-DISCOVERY%' or @Message like '%INTER-SERVICE%')`
3. See structured, searchable logs with full context

---

## Comprehensive Testing (15 Minutes)

### Test 4: Service Discovery on Restart

**Simulate service restart:**
```bash
# Restart CatalogService
docker compose restart catalogservice

# Wait for re-registration
sleep 10

# Make another loan request
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId": 2}'

# Check logs - should see new service discovery
docker logs libhub-loanservice-1 --tail 20 | grep "SERVICE-DISCOVERY"
```

✅ **Expected:** LoanService successfully discovers the restarted CatalogService

---

### Test 5: Service Unavailable Scenario

**Stop CatalogService:**
```bash
docker compose stop catalogservice
```

**Attempt to borrow:**
```bash
curl -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId": 1}'
```

**Expected Response:**
```json
{
  "message": "An error occurred while processing your request",
  "statusCode": 500
}
```

**Check logs:**
```bash
docker logs libhub-loanservice-1 --tail 10
```

✅ **Expected logs:**
```
❌ [SERVICE-DISCOVERY] No healthy instances found for service: catalogservice
❌ [INTER-SERVICE] Failed to get book 1 from CatalogService
```

**Restart service:**
```bash
docker compose start catalogservice
```

---

### Test 6: Multiple Requests (Load Test)

**Create test script** (`test-service-discovery-load.sh`):
```bash
#!/bin/bash

# Get token
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"sdtest","password":"Test123!"}' \
  | grep -o '"token":"[^"]*' | sed 's/"token":"//')

echo "Running 10 loan requests..."

for i in {1..10}; do
  echo "Request $i..."
  curl -s -X POST http://localhost:5000/api/loans \
    -H "Content-Type: application/json" \
    -H "Authorization: Bearer $TOKEN" \
    -d "{\"bookId\": $((i % 5 + 1))}" > /dev/null
  echo "✓ Request $i complete"
  sleep 0.5
done

echo ""
echo "Complete! Check logs:"
echo "docker logs libhub-loanservice-1 --tail 50 | grep SERVICE-DISCOVERY"
```

**Run:**
```bash
chmod +x test-service-discovery-load.sh
./test-service-discovery-load.sh
```

**View aggregated logs:**
```bash
docker logs libhub-loanservice-1 --tail 100 | grep -c "SERVICE-DISCOVERY"
```

✅ **Expected:** See 20+ service discovery events (2 per loan: GetBook + DecrementStock)

---

### Test 7: Seq Analysis

**Step-by-step Seq analysis:**

1. **Open Seq:** http://localhost:5341

2. **Basic filter:**
   ```
   ServiceName = 'LoanService'
   ```

3. **Service Discovery only:**
   ```
   @Message like '%SERVICE-DISCOVERY%'
   ```

4. **Inter-service communication:**
   ```
   @Message like '%INTER-SERVICE%'
   ```

5. **Full flow for a specific loan:**
   ```
   CorrelationId = 'YOUR_CORRELATION_ID_HERE'
   ```

6. **Failed service discoveries:**
   ```
   @Level = 'Error' and @Message like '%SERVICE-DISCOVERY%'
   ```

7. **Performance analysis:**
   - Click on "🔍 [SERVICE-DISCOVERY]" log entry
   - Check timestamp difference with next "✅ [SERVICE-DISCOVERY]" log
   - Typical Consul query should be < 50ms

---

## Verification Checklist

### ✅ Basic Functionality
- [ ] All services registered in Consul
- [ ] LoanService can borrow books successfully
- [ ] Service discovery logs appear in console
- [ ] Service discovery logs appear in Seq
- [ ] Correlation IDs propagate correctly

### ✅ Error Handling
- [ ] Graceful failure when CatalogService is down
- [ ] Error logs show "No healthy instances"
- [ ] Service recovers after CatalogService restart

### ✅ Observability
- [ ] Logs include service URL discovered from Consul
- [ ] Logs include ServiceId from Consul
- [ ] Logs include health status
- [ ] Structured logging works in Seq
- [ ] Can filter logs by correlation ID

### ✅ Performance
- [ ] Service discovery completes in < 100ms
- [ ] No noticeable latency in loan operations
- [ ] Multiple concurrent requests handled correctly

---

## Common Issues & Solutions

### Issue 1: "No healthy instances found"

**Cause:** CatalogService not registered or unhealthy

**Solution:**
```bash
# Check Consul
curl http://localhost:8500/v1/health/service/catalogservice

# Restart service
docker compose restart catalogservice

# Check health endpoint
curl http://catalogservice:5001/health
```

---

### Issue 2: Logs not showing service discovery

**Cause:** Wrong log level or filter

**Solution:**
```bash
# Check Serilog configuration in appsettings.json
# Ensure MinimumLevel is "Information" or lower

# View all LoanService logs
docker logs -f libhub-loanservice-1
```

---

### Issue 3: Seq not showing logs

**Cause:** Seq not reachable or wrong URL

**Solution:**
```bash
# Verify Seq is running
docker ps | grep seq

# Test Seq endpoint
curl http://localhost:5341

# Check Seq URL in appsettings.json
# Should be: "http://seq:5341" (not localhost in Docker)
```

---

### Issue 4: Service discovery returns wrong URL

**Cause:** Service registration issue

**Solution:**
```bash
# Check service registration in Consul
curl http://localhost:8500/v1/catalog/service/catalogservice | jq

# Verify ServiceConfig in docker-compose.yml
# Should have correct ServiceHost and ServicePort
```

---

## Performance Benchmarks

### Expected Metrics

| Metric | Target | Acceptable |
|--------|--------|------------|
| Consul query time | < 50ms | < 100ms |
| Service discovery + HTTP call | < 200ms | < 500ms |
| Total loan creation | < 1s | < 2s |

### Measure Performance

**In Seq:**
1. Click on service discovery log
2. Note timestamp
3. Find corresponding "discovered service" log
4. Calculate difference

**Sample times:**
```
[10:15:23.123] 🔍 Querying Consul
[10:15:23.145] ✅ Discovered service  ← 22ms
```

---

## Success Criteria

### ✅ Implementation is successful if:

1. **Service Discovery Works**
   - LoanService discovers CatalogService via Consul
   - No hardcoded URLs in logs
   - Logs show `http://catalogservice:5001` (from Consul)

2. **Logging is Comprehensive**
   - Every service discovery logged with 🔍 and ✅
   - Every inter-service call logged with 🔗 and 📨
   - Errors logged with ❌
   - All logs have correlation IDs

3. **Resilience**
   - System handles CatalogService restart gracefully
   - Errors are caught and logged properly
   - Service recovers automatically

4. **Observability**
   - Can trace full loan flow in Seq
   - Can identify service discovery issues quickly
   - Can monitor performance via timestamps

---

## Quick Verification Commands

**One-liner to test everything:**
```bash
# Register, login, borrow, and check logs
curl -s -X POST http://localhost:5000/api/users/register \
  -H "Content-Type: application/json" \
  -d '{"username":"quicktest","email":"qt@test.com","password":"Test123!"}' && \
TOKEN=$(curl -s -X POST http://localhost:5000/api/users/login \
  -H "Content-Type: application/json" \
  -d '{"username":"quicktest","password":"Test123!"}' | grep -o '"token":"[^"]*' | sed 's/"token":"//') && \
curl -s -X POST http://localhost:5000/api/loans \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer $TOKEN" \
  -d '{"bookId": 1}' && \
echo "" && \
echo "=== SERVICE DISCOVERY LOGS ===" && \
docker logs libhub-loanservice-1 --tail 20 | grep -E "SERVICE-DISCOVERY|INTER-SERVICE"
```

**Check Consul health:**
```bash
curl -s http://localhost:8500/v1/health/service/catalogservice | jq '.[0].Checks[] | {Status: .Status, ServiceName: .ServiceName}'
```

**Count service discoveries in last hour:**
```bash
docker logs libhub-loanservice-1 --since 1h | grep -c "SERVICE-DISCOVERY"
```

---

## Additional Resources

- **Consul UI:** http://localhost:8500
- **Seq Logs:** http://localhost:5341
- **Gateway:** http://localhost:5000
- **Frontend:** http://localhost:8080

---

## Troubleshooting Tips

1. **Always check Consul first** - If service discovery fails, verify service is in Consul
2. **Use Seq for correlation** - Much easier to trace requests than console logs
3. **Check health endpoints** - `/health` on each service should return 200
4. **Watch docker logs in real-time** - `docker logs -f` to see live service discovery
5. **Verify network connectivity** - All services should be on `libhub-network`

---

## Summary

This testing guide covers:
- ✅ Basic service discovery verification
- ✅ Error handling scenarios
- ✅ Performance monitoring
- ✅ Observability validation
- ✅ Troubleshooting common issues

**Expected time to complete:** 15-20 minutes

**Status:** Ready for testing! 🚀
