# Phase 4 Testing Guide: Seq Integration

## Prerequisites

Ensure all services are running:
```bash
docker ps
```

You should see all 8 containers running:
- libhub-seq
- libhub-mysql
- libhub-consul
- libhub-ver2-userservice-1
- libhub-ver2-catalogservice-1
- libhub-ver2-loanservice-1
- libhub-gateway
- libhub-frontend

---

## Test 1: Access Seq Web UI

### Steps:
1. Open your browser
2. Navigate to: **http://localhost:5341**

### Expected Result:
- Seq welcome screen appears
- You should see a stream of logs appearing in real-time
- Interface shows:
  - Search bar at top
  - Log entries in the center
  - Filters and properties on the right

### What to Look For:
- ✅ Logs from multiple services (look for `ServiceName` property)
- ✅ Different log levels (Information, Warning, Error)
- ✅ Timestamps are recent
- ✅ Structured properties visible (click on any log entry to expand)

**Screenshot opportunity:** This is your Seq dashboard!

---

## Test 2: Filter Logs by Service

### Steps:
1. In Seq UI, find the search bar at the top
2. Enter this query:
   ```
   ServiceName = 'UserService'
   ```
3. Press Enter or click the search button

### Expected Result:
- Only logs from UserService appear
- All other service logs are filtered out
- You should see logs like:
  - Starting UserService
  - Database created
  - Consul registration
  - Health checks (if DEBUG level enabled)

### Try Other Services:
```
ServiceName = 'LoanService'
ServiceName = 'CatalogService'
ServiceName = 'Gateway'
```

### Bonus: Filter Multiple Services
```
ServiceName in ['LoanService', 'CatalogService']
```

---

## Test 3: View Consul Registration Events

### Steps:
1. Clear any existing filters (click X on search bar)
2. Enter this query:
   ```
   @MessageTemplate like '%CONSUL%'
   ```
3. Press Enter

### Expected Result:
You should see logs like:
```
🔌 [CONSUL-REGISTER] Service: userservice | ID: ... | Address: userservice:5002 | Health: http://userservice:5002/health | Attempt: 1/5
✅ [CONSUL-SUCCESS] Service: userservice | ID: ... | Address: userservice:5002 registered successfully
```

### What to Verify:
- ✅ All 3 services (userservice, catalogservice, loanservice) registered
- ✅ Emojis display correctly (🔌, ✅)
- ✅ Each service has `[CONSUL-REGISTER]` and `[CONSUL-SUCCESS]` logs
- ✅ Can expand each log to see structured properties

---

## Test 4: Track Complete Request Journey (Correlation ID)

This is the **most powerful feature** - tracking a single request across all services!

### Steps:

#### 4.1: Perform a Borrow Operation

1. Open frontend: **http://localhost:8080**
2. Login with:
   - Email: `john.doe@example.com`
   - Password: `password123`
3. Click on any book and borrow it
4. Open browser DevTools (F12)
5. Go to Console tab
6. Look for log: `🔍 Track request: req-...`
7. **Copy the correlation ID** (e.g., `req-1699123456-abc123`)

#### 4.2: Search in Seq

1. Go back to Seq UI (http://localhost:5341)
2. Enter this query (replace with your actual correlation ID):
   ```
   CorrelationId = 'req-1699123456-abc123'
   ```
3. Press Enter

### Expected Result:

You should see the **complete journey** of that request across all services:

```
Timeline (chronological order):

[Gateway] Request started: POST /api/loans
[Gateway] ✅ [JWT-SUCCESS] Token validated | UserId: 5 | Email: john.doe@example.com | Role: User
[Gateway] Request completed: POST /api/loans - 200

[LoanService] Request started: POST /api/loans
[LoanService] 🚀 [SAGA-START] BorrowBook | UserId: 5 | BookId: 1
[LoanService] 📝 [SAGA-STEP-1] Loan record created | LoanId: 42 | Status: PENDING
[LoanService] 🔍 [SAGA-STEP-2] Checking book availability | BookId: 1
[LoanService] 🔗 Calling CatalogService: GET /api/books/1

[CatalogService] Request started: GET /api/books/1
[CatalogService] Request completed: GET /api/books/1 - 200 (15ms)

[LoanService] 📨 CatalogService response: 200
[LoanService] ✅ [SAGA-STEP-2-SUCCESS] Book is available | BookId: 1 | AvailableCopies: 5
[LoanService] 📉 [SAGA-STEP-3] Decrementing book stock | BookId: 1
[LoanService] 🔗 Calling CatalogService: PUT /api/books/1/stock (decrement)

[CatalogService] Request started: PUT /api/books/1/stock
[CatalogService] 📦 [STOCK-UPDATE-START] decrement stock | BookId: 1 | ChangeAmount: -1
[CatalogService] ✅ [STOCK-UPDATE-SUCCESS] Stock updated | BookId: 1 | ChangeAmount: -1
[CatalogService] Request completed: PUT /api/books/1/stock - 204 (35ms)

[LoanService] 📨 CatalogService response: 204
[LoanService] ✅ [SAGA-STEP-3-SUCCESS] Stock decremented successfully | BookId: 1
[LoanService] 🎉 [SAGA-SUCCESS] Borrow completed | LoanId: 42 | UserId: 5 | BookId: 1 | DueDate: 2025-11-23
[LoanService] Request completed: POST /api/loans - 200 (1200ms)
```

### What to Verify:
- ✅ Same CorrelationId in ALL logs
- ✅ Logs from Gateway, LoanService, and CatalogService
- ✅ Chronological order (oldest first)
- ✅ All emojis visible
- ✅ Structured properties (UserId, BookId, LoanId) visible
- ✅ Can expand each log entry for full details

**This is the killer feature of the logging system!** 🚀

---

## Test 5: Search for Authentication Events

### Steps:
1. In Seq, enter this query:
   ```
   @MessageTemplate like '%LOGIN%'
   ```
2. Press Enter

### Expected Result:
You should see logs like:
```
🔐 [LOGIN-ATTEMPT] Login attempt | Email: john.doe@example.com
✅ [LOGIN-SUCCESS] User logged in successfully | Email: john.doe@example.com
```

### Try Different Queries:

**Failed logins:**
```
@MessageTemplate like '%LOGIN-FAILED%'
```

**All authentication events:**
```
@MessageTemplate like '%LOGIN%' or @MessageTemplate like '%REGISTER%'
```

**Successful registrations:**
```
@MessageTemplate like '%REGISTER-SUCCESS%'
```

---

## Test 6: Find All Saga Operations

### Steps:
1. In Seq, enter:
   ```
   @MessageTemplate like '%SAGA%'
   ```
2. Press Enter

### Expected Result:
All saga-related logs appear:
- 🚀 [SAGA-START]
- 📝 [SAGA-STEP-1]
- 🔍 [SAGA-STEP-2]
- 📉 [SAGA-STEP-3]
- 🎉 [SAGA-SUCCESS]
- 💥 [SAGA-FAILED] (if any failures)
- 🔄 [SAGA-COMPENSATION] (if any rollbacks)

### Drill Down to Specific Saga:
Click on any `[SAGA-START]` log and note the CorrelationId, then search:
```
CorrelationId = 'req-...'
```

---

## Test 7: Monitor Errors and Failures

### Steps:
1. In Seq, enter:
   ```
   @Level = 'Error'
   ```
2. Press Enter

### Expected Result:
- Only error-level logs appear
- Should be very few (or zero) in a healthy system

### Find Warnings and Errors:
```
@Level in ['Warning', 'Error']
```

### Find Failed Operations:
```
@MessageTemplate like '%FAILED%'
```

### Find Saga Failures:
```
@MessageTemplate like '%SAGA-FAILED%' and @Level = 'Error'
```

---

## Test 8: Performance Monitoring

### Steps:
1. In Seq, enter:
   ```
   ElapsedMs is not null
   ```
2. Press Enter

### Expected Result:
- Logs showing HTTP request durations
- Logs showing "Request completed" with timing

### Find Slow Requests (> 500ms):
```
ElapsedMs > 500
```

### Find Very Slow Requests (> 2 seconds):
```
ElapsedMs > 2000
```

---

## Test 9: Real-Time Log Streaming

### Steps:
1. In Seq UI, look at the top-right corner
2. Click the **"Stream"** button (may show as a play icon)
3. Keep Seq UI visible

### Test It:
1. Go to frontend (http://localhost:8080)
2. Perform some operations:
   - Browse books
   - Login/logout
   - Borrow a book
   - Return a book

### Expected Result:
- Logs appear in Seq **in real-time** as operations happen
- New logs automatically scroll into view
- Like `docker logs -f` but much better!

**Tip:** This is great for watching system behavior during development!

---

## Test 10: Filter by User Actions

### Steps:

#### Find All Operations by Specific User:
```
UserId = 5
```

#### Find All Operations on Specific Book:
```
BookId = 1
```

#### Find User 5's Operations on Book 1:
```
UserId = 5 and BookId = 1
```

#### Find All Active Loans:
```
LoanId is not null and @MessageTemplate like '%SAGA-SUCCESS%'
```

---

## Test 11: Time-Based Searches

### Find Logs from Last Hour:
```
@Timestamp > DateTime.UtcNow.AddHours(-1)
```

### Find Logs from Last 5 Minutes:
```
@Timestamp > DateTime.UtcNow.AddMinutes(-5)
```

### Find Logs from Today:
```
@Timestamp > DateTime.Today
```

### Find Logs from Specific Time Range:
```
@Timestamp > DateTime('2025-11-09T10:00:00Z') and @Timestamp < DateTime('2025-11-09T12:00:00Z')
```

---

## Test 12: Combined Queries (Advanced)

### Successful Borrow Operations by User 5:
```
UserId = 5 and @MessageTemplate like '%SAGA-SUCCESS%'
```

### Failed Operations in Last Hour:
```
@Level = 'Error' and @Timestamp > DateTime.UtcNow.AddHours(-1)
```

### JWT Validation Failures:
```
@MessageTemplate like '%JWT-FAILED%' and ServiceName = 'Gateway'
```

### Stock Updates for Book 1:
```
BookId = 1 and @MessageTemplate like '%STOCK-UPDATE%'
```

### All Health Checks (if DEBUG enabled):
```
@MessageTemplate like '%HEALTH-CHECK%'
```

---

## Test 13: Export Logs

### Steps:
1. Perform any search query
2. Look for the **"Export"** or **"Download"** button (usually near search)
3. Choose format (JSON or CSV)
4. Download the logs

### Use Case:
- Share specific logs with team
- Attach to bug reports
- Analyze logs offline

---

## Test 14: Create Saved Queries (Optional)

### Steps:
1. Perform a useful search (e.g., `@MessageTemplate like '%SAGA%'`)
2. Look for **"Save"** or **"Star"** icon
3. Give it a name (e.g., "All Saga Operations")
4. Access it later from saved queries dropdown

### Recommended Queries to Save:
- All Saga Operations: `@MessageTemplate like '%SAGA%'`
- Authentication Events: `@MessageTemplate like '%LOGIN%' or @MessageTemplate like '%REGISTER%'`
- Consul Events: `@MessageTemplate like '%CONSUL%'`
- Errors Only: `@Level = 'Error'`
- Slow Requests: `ElapsedMs > 1000`

---

## Test 15: Verify Log Persistence

### Steps:
1. Note the current time: `__________`
2. Perform some operations (borrow book, etc.)
3. Restart all services:
   ```bash
   docker compose down
   docker compose up -d
   ```
4. Wait for services to start (~20 seconds)
5. Open Seq UI: http://localhost:5341
6. Search for logs from before restart:
   ```
   @Timestamp > DateTime('YYYY-MM-DDTHH:MM:SSZ')
   ```
   (Use the time you noted in step 1)

### Expected Result:
- ✅ Logs from before restart are still visible
- ✅ New logs appear after services restart
- ✅ No data loss

**This proves the `seq-data` volume is working!**

---

## Troubleshooting

### Issue: Seq UI Not Accessible

**Check:**
```bash
# Is Seq running?
docker ps | grep seq

# Check Seq logs
docker logs libhub-seq

# Try accessing API
curl http://localhost:5341/api
```

**Solution:**
```bash
docker compose restart seq
```

---

### Issue: No Logs Appearing in Seq

**Check:**
```bash
# Are services configured correctly?
docker exec libhub-ver2-userservice-1 env | grep Serilog

# Check service logs for Seq connection errors
docker logs libhub-ver2-userservice-1 | grep -i seq
```

**Solution:**
```bash
# Restart services
docker compose restart userservice catalogservice loanservice gateway
```

---

### Issue: Logs Only in Console, Not in Seq

**Verify environment variable:**
```bash
docker exec libhub-ver2-userservice-1 env | grep "Serilog__WriteTo__1__Args__serverUrl"
```

Should output: `http://seq:80`

**If not set:**
```bash
docker compose down
docker compose up -d
```

---

### Issue: Old Logs Not Visible

**Retention Policy:**
- Default: 7 days
- Logs older than 7 days are automatically deleted
- This is configured in `SEQ_FIRSTRUN_RETENTION_DAYS=7`

**To keep logs longer:**
Edit `docker-compose.yml`:
```yaml
environment:
  - SEQ_FIRSTRUN_RETENTION_DAYS=30  # Keep for 30 days
```

---

## Success Checklist

After completing all tests, you should have verified:

- [x] Seq UI accessible at http://localhost:5341
- [x] Logs from all 4 services visible
- [x] Can filter by ServiceName
- [x] Can search by CorrelationId
- [x] Complete request journey traceable
- [x] Authentication events logged
- [x] Saga operations visible with emojis
- [x] Errors and failures searchable
- [x] Real-time log streaming works
- [x] Time-based searches work
- [x] Logs persist across restarts
- [x] Can export logs
- [x] Performance monitoring possible

---

## Quick Reference: Useful Queries

Copy these for quick use:

```
# By Service
ServiceName = 'UserService'
ServiceName = 'CatalogService'
ServiceName = 'LoanService'
ServiceName = 'Gateway'

# By Log Level
@Level = 'Error'
@Level = 'Warning'
@Level in ['Warning', 'Error']

# By Event Type
@MessageTemplate like '%CONSUL%'
@MessageTemplate like '%SAGA%'
@MessageTemplate like '%JWT%'
@MessageTemplate like '%LOGIN%'
@MessageTemplate like '%STOCK%'

# By Entity
UserId = 5
BookId = 1
LoanId = 42
CorrelationId = 'req-...'

# By Time
@Timestamp > DateTime.UtcNow.AddHours(-1)
@Timestamp > DateTime.UtcNow.AddMinutes(-15)
@Timestamp > DateTime.Today

# Performance
ElapsedMs > 500
ElapsedMs > 1000
ElapsedMs > 2000

# Combined
UserId = 5 and @MessageTemplate like '%SAGA%'
@Level = 'Error' and @Timestamp > DateTime.UtcNow.AddHours(-1)
ServiceName = 'Gateway' and @MessageTemplate like '%JWT-FAILED%'
```

---

## What's Next?

🎉 **Congratulations!** You've completed all 4 phases of the logging enhancement:

1. ✅ **Phase 1:** Structured logging with Serilog
2. ✅ **Phase 2:** Correlation IDs for request tracing
3. ✅ **Phase 3:** Rich event logging with emojis
4. ✅ **Phase 4:** Centralized log aggregation with Seq

**Your system now has:**
- Complete observability across all microservices
- Ability to trace requests end-to-end
- Easy debugging with powerful search
- Real-time monitoring capabilities
- Persistent log storage

**Use Seq daily for:**
- Debugging issues
- Monitoring system health
- Understanding user behavior
- Performance optimization
- Troubleshooting production issues

---

## Pro Tips

1. **Keep Seq UI Open:** When developing, keep Seq in a browser tab with Stream mode on
2. **Use Saved Queries:** Save your most common searches for quick access
3. **Monitor Errors:** Check `@Level = 'Error'` daily
4. **Track Performance:** Watch `ElapsedMs` to catch slow operations early
5. **Export for Bug Reports:** Export relevant logs when reporting issues
6. **Correlation ID is Key:** Always use CorrelationId for end-to-end tracing

---

**Happy Logging! 🚀**
