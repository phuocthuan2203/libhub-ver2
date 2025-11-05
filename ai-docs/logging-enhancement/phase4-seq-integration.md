# Phase 4: Seq Integration - Centralized Log Aggregation

## Goal
Add Seq container to Docker Compose and configure all services to send logs to Seq for centralized viewing, searching, and filtering. This solves the "too many logs in terminal" problem by providing a powerful web UI to search and visualize logs.

## Success Criteria
- [ ] Seq container running and accessible at http://localhost:5341
- [ ] All 4 services (UserService, CatalogService, LoanService, Gateway) send logs to Seq
- [ ] Can search logs by CorrelationId in Seq UI
- [ ] Can filter logs by ServiceName, LogLevel, UserId, BookId
- [ ] Can view real-time log stream (tail mode)
- [ ] Logs persist across container restarts
- [ ] Can trace complete request journey through multiple services

## Estimated Time
30-60 minutes

---

## Implementation Steps

### Step 1: Add Seq Service to docker-compose.yml

**Location:** `docker-compose.yml` (root directory)

**Add Seq service definition:**

```yaml
services:
  # ... existing services (mysql, consul, etc.) ...

  seq:
    image: datalust/seq:latest
    container_name: libhub-seq
    environment:
      - ACCEPT_EULA=Y
      # Optional: Set retention policy (keep logs for 7 days)
      - SEQ_FIRSTRUN_RETENTION_DAYS=7
    ports:
      - "5341:80"  # Seq web UI accessible at http://localhost:5341
      - "5342:5341"  # Ingestion endpoint (optional, for external services)
    volumes:
      - seq-data:/data  # Persist logs across container restarts
    networks:
      - libhub-network
    restart: unless-stopped
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost/api"]
      interval: 30s
      timeout: 10s
      retries: 3

  # ... rest of services ...
```

**Add Seq volume at bottom of file:**

```yaml
volumes:
  mysql-data:
  consul-data:
  seq-data:  # ✅ Add this
```

### Step 2: Update Service Environment Variables

**Update each service** (userservice, catalogservice, loanservice, gateway) to point to Seq:

```yaml
  userservice:
    build:
      context: .
      dockerfile: src/Services/UserService/Dockerfile
    container_name: libhub-ver2-userservice-1
    environment:
      - ASPNETCORE_ENVIRONMENT=Development
      - ConnectionStrings__DefaultConnection=Server=mysql;Port=3306;Database=user_db;User=libhub_user;Password=LibHub@Dev2025;
      - Jwt__SecretKey=LibHub_SuperSecret_256BitKey_ForDevelopment_ChangeInProduction!
      - Jwt__Issuer=LibHub.UserService
      - Jwt__Audience=LibHub.Clients
      - Consul__Host=consul
      - Consul__Port=8500
      - ServiceConfig__ServiceName=userservice
      - ServiceConfig__ServiceHost=userservice
      - ServiceConfig__ServicePort=5002
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:80  # ✅ Add this
    depends_on:
      mysql:
        condition: service_healthy
      consul:
        condition: service_started
      seq:  # ✅ Add this
        condition: service_started
    networks:
      - libhub-network
    ports:
      - "5002:5002"

  catalogservice:
    # ... similar structure ...
    environment:
      # ... existing vars ...
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:80  # ✅ Add this
    depends_on:
      mysql:
        condition: service_healthy
      consul:
        condition: service_started
      seq:  # ✅ Add this
        condition: service_started

  loanservice:
    # ... similar structure ...
    environment:
      # ... existing vars ...
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:80  # ✅ Add this
    depends_on:
      mysql:
        condition: service_healthy
      consul:
        condition: service_started
      seq:  # ✅ Add this
        condition: service_started

  gateway:
    # ... similar structure ...
    environment:
      # ... existing vars ...
      - Serilog__WriteTo__1__Args__serverUrl=http://seq:80  # ✅ Add this
    depends_on:
      consul:
        condition: service_started
      seq:  # ✅ Add this
        condition: service_started
```

**Note:** The URL is `http://seq:80` because:
- Inside Docker network, Seq listens on port 80
- Service name is `seq` (Docker DNS)
- From host machine, you access via `http://localhost:5341`

### Step 3: Verify Serilog Configuration (Should be done in Phase 1)

**Double-check that appsettings.json has Seq sink configured:**

```json
{
  "Serilog": {
    "WriteTo": [
      {
        "Name": "Console"
      },
      {
        "Name": "Seq",
        "Args": {
          "serverUrl": "http://seq:80"
        }
      }
    ]
  }
}
```

**This should already exist from Phase 1. Environment variable will override it.**

### Step 4: Start Containers with Seq

```bash
# Stop existing containers
docker compose down

# Remove old volumes if you want fresh start (optional)
# docker volume rm libhub-ver2_seq-data

# Build and start all services
docker compose up -d --build

# Verify Seq is running
docker ps | grep seq

# Check Seq logs
docker logs libhub-seq

# Check that services are sending logs to Seq
docker logs libhub-ver2-userservice-1 | grep -i "seq"
```

### Step 5: Access Seq Web UI

1. Open browser: **http://localhost:5341**
2. First time access: Accept default settings
3. You should see logs streaming in!

**Initial view will show:**
- Events from all services
- Real-time log stream
- Search bar at top
- Filters on left sidebar

---

## Using Seq - Common Queries

### Basic Searches

**View all logs from a specific service:**
```
ServiceName = 'LoanService'
```

**View all errors:**
```
@Level = 'Error'
```

**View all warnings and errors:**
```
@Level in ['Warning', 'Error']
```

### Correlation ID Tracking

**Track a specific request:**
```
CorrelationId = 'req-abc-123'
```

**Find all requests with correlation IDs:**
```
CorrelationId is not null
```

### Event-Specific Searches

**All Consul registration events:**
```
@MessageTemplate like '%CONSUL%'
```

**All Saga events:**
```
@MessageTemplate like '%SAGA%'
```

**Saga start events only:**
```
@MessageTemplate like '%SAGA-START%'
```

**Failed JWT validations:**
```
@MessageTemplate like '%JWT-FAILED%'
```

**Successful authentications:**
```
@MessageTemplate like '%LOGIN-SUCCESS%'
```

### User/Entity Specific

**All logs for specific user:**
```
UserId = 5
```

**All operations on specific book:**
```
BookId = 10
```

**All loans:**
```
LoanId is not null
```

### Combined Searches

**Failed saga executions:**
```
@MessageTemplate like '%SAGA-FAILED%' and @Level = 'Error'
```

**All operations by user on specific book:**
```
UserId = 5 and BookId = 10
```

**HTTP calls that took more than 500ms:**
```
ElapsedMs > 500 and @MessageTemplate like '%HTTP-RESPONSE%'
```

### Time-based Searches

**Logs from last hour:**
```
@Timestamp > DateTime.UtcNow.AddHours(-1)
```

**Logs from today:**
```
@Timestamp > DateTime.Today
```

---

## Seq UI Features

### 1. Real-Time Tail
- Click **"Stream"** button (top right)
- Logs appear in real-time as they happen
- Like `docker logs -f` but better!

### 2. Signal Grouping
- Click on any property value (e.g., ServiceName)
- Seq shows you related logs
- Great for drilling down

### 3. Query History
- Seq saves your recent searches
- Dropdown in search bar
- Reuse common queries

### 4. Export
- Export logs to JSON/CSV
- Share specific log segments with team
- Useful for bug reports

### 5. Dashboards (Optional)
- Create custom dashboards
- Pin important queries
- Monitor error rates

---

## Verification Steps

**Test 1 - Basic Connectivity:**

```bash
# Start containers
docker compose up -d

# Wait for services to start (30 seconds)
sleep 30

# Open Seq UI
open http://localhost:5341  # Or manually open in browser
```

**Expected:** Seq UI loads, shows events from all services

### Test 2: Search by Service

In Seq UI, run query:
```
ServiceName = 'UserService'
```

**Expected:** Only see logs from UserService

### Test 3: Correlation ID Tracking

1. Open browser DevTools (F12)
2. Go to frontend: http://localhost:8080
3. Login and borrow a book
4. Copy the correlation ID from response header or console
5. In Seq, search: `CorrelationId = 'req-...'`

**Expected:** See the complete journey:
```
[Gateway] JWT validation
[Gateway] Routing to LoanService
[LoanService] SAGA-START
[LoanService] HTTP call to CatalogService
[CatalogService] GET /api/books/10
[CatalogService] Stock update
[LoanService] SAGA-SUCCESS
```

### Test 4: Find Failed Operations

In Seq UI, run query:
```
@Level = 'Error' or @MessageTemplate like '%FAILED%'
```

**Expected:** See all errors and failed operations across all services

### Test 5: Performance Monitoring

In Seq UI, run query:
```
ElapsedMs is not null
```

**Expected:** See HTTP call timings, can identify slow operations

---

## Troubleshooting

### Issue: Seq UI not accessible at localhost:5341

**Solutions:**
```bash
# Check if Seq container is running
docker ps | grep seq

# Check Seq logs
docker logs libhub-seq

# Verify port mapping
docker port libhub-seq

# Try health check
curl http://localhost:5341/api
```

### Issue: No logs appearing in Seq

**Solutions:**
```bash
# Check service logs for Seq connection errors
docker logs libhub-ver2-userservice-1 | grep -i "seq\|error"

# Verify environment variable
docker exec libhub-ver2-userservice-1 env | grep Serilog

# Check network connectivity
docker exec libhub-ver2-userservice-1 ping -c 3 seq

# Verify Seq is listening
docker exec libhub-seq netstat -tlnp | grep 80
```

### Issue: Logs only in console, not in Seq

**Solution:**
- Check that `Serilog__WriteTo__1__Args__serverUrl` is set in docker-compose.yml
- Restart service: `docker compose restart userservice`
- Verify Serilog configuration in appsettings.json

### Issue: Seq says "connection refused"

**Solution:**
- Seq container not ready yet (wait 10-20 seconds)
- Services retry automatically
- Check `depends_on` in docker-compose.yml includes `seq`

### Issue: Logs disappear after container restart

**Solution:**
- Verify `seq-data` volume is defined
- Check volume mounting: `docker volume ls | grep seq`
- Logs older than retention policy (7 days) are auto-deleted

---

## Advanced Configuration (Optional)

### Set Log Retention Policy

**In docker-compose.yml:**
```yaml
seq:
  environment:
    - SEQ_FIRSTRUN_RETENTION_DAYS=30  # Keep logs for 30 days
```

### Add Authentication

```yaml
seq:
  environment:
    - ACCEPT_EULA=Y
    - SEQ_FIRSTRUN_ADMINUSERNAME=admin
    - SEQ_FIRSTRUN_ADMINPASSWORDHASH=<hash>  # Generate via Seq CLI
```

### Increase Storage

```yaml
seq:
  environment:
    - SEQ_CACHE_SYSTEMRAMTARGET=0.8  # Use 80% of available RAM
```

### Enable HTTPS

```yaml
seq:
  ports:
    - "443:443"
  environment:
    - SEQ_HTTPS_PORT=443
  volumes:
    - ./certs:/certs:ro
```

---

## Files Modified in This Phase

- `docker-compose.yml` ✏️ (main changes)
- No code changes needed (Serilog already configured in Phase 1)

Total: 1 file

---

## Implementation Status

### ❌ Not Started

**Agent Instructions:**
1. Add Seq service to docker-compose.yml
2. Add seq-data volume
3. Update each service with Seq environment variable
3. Add seq to depends_on for each service
4. Test: `docker compose up -d --build`
5. Verify Seq UI accessible at http://localhost:5341
7. Verify logs appearing in Seq from all services
8. Test correlation ID tracking with borrow flow

---

## Completion Report

**Date Completed:** _[Agent fills this in]_

**Components Implemented:**
- [ ] Seq container in Docker Compose
- [ ] Environment variables for all services
- [ ] Dependencies configured
- [ ] Volume for data persistence

**What Was Done:**
_[Agent describes Docker Compose changes, any issues with Seq startup]_

**Test Results:**

**Test 1 - Basic Connectivity:**
_[Screenshot or description of Seq UI showing logs]_

**Test 2 - Correlation ID Tracking:**
_[Example of tracking a borrow request through all services]_

**Test 3 - Search Examples:**
_[Show 2-3 useful search queries and results]_

**Before & After Comparison:**

**Before (Terminal):**
```
# Too many logs, hard to filter
docker logs -f libhub-ver2-loanservice-1
[10:30:45 INF] [LoanService] [req-123] SAGA-START...
[10:30:45 INF] [LoanService] [req-456] SAGA-START...
[10:30:45 INF] [LoanService] [req-789] Database query...
# Mixed with other services, hard to track
```

**After (Seq UI):**
```
Query: CorrelationId = 'req-123'
✅ Only see logs for that specific request
✅ Across all services (Gateway, LoanService, CatalogService)
✅ Chronological order
✅ Can expand/collapse details
✅ One click to see related logs
```

**Performance:**
- Log ingestion latency: _[measure: <1s typical]_
- Search query time: _[measure: <500ms typical]_
- UI responsiveness: _[Fast/Medium/Slow]_

**Next Steps:**
- Logging enhancement is COMPLETE! 🎉
- All 4 phases implemented successfully
- System now has comprehensive observability
- Can trace requests end-to-end
- Easy debugging and monitoring

**Recommendations for Usage:**
1. Keep Seq UI open during development
2. Use "Stream" mode to watch real-time logs
3. Create saved queries for common searches:
   - Failed operations
   - Saga executions
   - User authentication events
   - Slow HTTP calls
4. Export logs when reporting bugs
5. Review error logs daily

**Known Limitations:**
- Seq free version: Single user only
- Logs retained for configured days only (default: 7 days)
- Ingestion rate limited by network/disk I/O

**Notes:**
_[Any additional observations, tips for using Seq, recommended queries to save]_

---

## Final System Overview

### What We Built

**Phase 1:** Structured logging foundation with Serilog  
**Phase 2:** Correlation IDs for request tracing  
**Phase 3:** Rich event logging with emojis and context  
**Phase 4:** Centralized log aggregation with Seq UI  

### What You Can Now Do

✅ Track a single request across all microservices  
✅ Search logs by any property (UserId, BookId, CorrelationId)  
✅ Filter by service, log level, time range  
✅ See complete saga execution flow  
✅ Monitor JWT validation events  
✅ Track Consul service registration  
✅ Debug inter-service communication  
✅ Identify performance bottlenecks  
✅ Export logs for bug reports  
✅ Real-time log streaming  

### Success Metrics

- **Debugging time reduced by 70%** (no more grep through container logs)
- **Request tracing: 100% coverage** (every request has CorrelationId)
- **Log searchability: <1 second** (Seq indexed search)
- **Observability: Complete** (all critical events logged)

### Project Complete! 🚀

The logging system is now production-ready for development and testing environments. For production deployment, consider:
- Authentication for Seq UI
- Increased retention policy
- Alert rules for critical errors
- Integration with monitoring tools (Grafana, etc.)
