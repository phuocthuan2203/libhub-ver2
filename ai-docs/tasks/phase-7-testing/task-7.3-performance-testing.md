# Task 7.3: Performance Testing

**Phase**: 7 - System Integration & Testing  
**Type**: Load Testing  
**Estimated Time**: 3-4 hours  
**Dependencies**: Task 7.2 complete

---

## Objective

Verify system meets performance requirements:
- 200 concurrent users
- Search response time < 2 seconds

---

## Tool Selection

**Primary**: Apache JMeter (recommended)  
**Alternatives**: k6, Artillery, Gatling

**Install JMeter**:
wget https://dlcdn.apache.org//jmeter/binaries/apache-jmeter-5.6.3.tgz
tar -xzf apache-jmeter-5.6.3.tgz
cd apache-jmete

text

---

## Test Scenarios

### Test 1: Search Endpoint Load
**Config**:
- Endpoint: GET /api/books?search=fiction
- Users: 200 concurrent
- Duration: 5 minutes
- Ramp-up: 30 seconds

**Success Criteria**:
- Average response time < 2000ms ✅
- 95th percentile < 3000ms ✅
- Error rate < 1% ✅

---

### Test 2: Borrow Book (Saga Pattern)
**Config**:
- Workflow: Login → Borrow
- Users: 50 concurrent
- Duration: 3 minutes

**Success Criteria**:
- No database deadlocks ✅
- All Saga transactions complete ✅
- No "CheckedOut" with unavailable books ✅

---

### Test 3: Database Connection Pool
**Config**:
- Mixed requests (GET/POST)
- Users: 200 concurrent
- Duration: 10 minutes

**Monitor**:
mysql -e "SHOW STATUS LIKE 'Threads_connected';"

text

**Success Criteria**:
- No "Too many connections" errors ✅
- Connection pool adequate ✅

---

## AI Agent Task

**Generate JMeter test plan**:

**Prompt**:
Generate Apache JMeter test plan for LibHub performance testing.
Include 200 concurrent users hitting search endpoint for 5 minutes.
text

**Execute**: `./jmeter -n -t libhub-test.jmx -l results.jtl`

---

## Metrics to Collect

- Response time (avg, p95, p99)
- Throughput (requests/second)
- Error rate (%)
- Database connections
- CPU/Memory usage

---

## Performance Baselines

| Metric | Target | Actual | Status |
|--------|--------|--------|--------|
| Search response | <2s | ___ | Pending |
| Concurrent users | 200 | ___ | Pending |
| Error rate | <1% | ___ | Pending |
| Throughput | >100 RPS | ___ | Pending |

---

## If Performance Issues Found

**Response time > 2s**:
- Add database indexes
- Implement caching (Redis)
- Optimize SQL queries

**Error rate > 1%**:
- Increase connection pool
- Add retry logic
- Scale services

---

## Acceptance Criteria

- [ ] All 3 test scenarios executed
- [ ] 200 concurrent users supported
- [ ] Search responds in < 2 seconds
- [ ] No critical performance issues
- [ ] Results documented with graphs

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 7.3: Performance Testing (date)

200 concurrent users: PASS

Search < 2s: PASS (avg 1.2s)

Error rate: 0.3%

System meets all performance requirements

text

**Next**: Task 7.4 (Security Testing)
