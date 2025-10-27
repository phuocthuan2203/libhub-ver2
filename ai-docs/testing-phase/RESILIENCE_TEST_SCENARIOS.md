# Resilience & Failure Testing

**Objective**: Verify system handles failures gracefully

---

## Scenario 1: CatalogService Down During Borrow

**Setup**: Stop CatalogService
Stop CatalogService
pkill -f CatalogService.Api

text

**Test**:
1. User attempts to borrow book
2. Observe behavior

**Expected Results**:
- ✅ LoanService creates PENDING loan
- ✅ HTTP call to CatalogService fails
- ✅ Saga marks loan as FAILED (compensating transaction)
- ✅ User sees error message "Service temporarily unavailable"
- ✅ No loan with status "CheckedOut" created

**Verification**:
SELECT Status FROM loan_db.Loans WHERE LoanId=<id>;
-- Should be "FAILED", not "CheckedOut"

text

**Priority**: CRITICAL (tests Saga resilience)

---

## Scenario 2: Database Connection Lost

**Setup**: Simulate DB connection loss
-- Revoke connection privileges temporarily
REVOKE ALL ON catalog_db.* FROM 'libhub_user'@'localhost';

text

**Test**:
1. User attempts to search books
2. Observe behavior

**Expected Results**:
- ✅ Service returns 500 Internal Server Error
- ✅ Error logged with details
- ✅ System doesn't crash
- ✅ Connection retry attempted

**Cleanup**:
GRANT ALL ON catalog_db.* TO 'libhub_user'@'localhost';

text

---

## Scenario 3: Gateway Down

**Setup**: Stop Gateway
pkill -f Gateway.Api

text

**Test**:
1. Frontend attempts to call API
2. Observe behavior

**Expected Results**:
- ✅ Frontend displays connection error
- ✅ No JavaScript errors in console
- ✅ Graceful degradation

---

## Scenario 4: High Database Load

**Setup**: Create artificial load
-- Lock table
LOCK TABLES catalog_db.Books WRITE;
-- Wait 10 seconds
SELECT SLEEP(10);
UNLOCK TABLES;

text

**Test**:
1. User searches books during lock
2. Measure response time

**Expected Results**:
- ✅ Request waits but doesn't timeout immediately
- ✅ Eventually completes or returns timeout error
- ✅ No database deadlock

---

## Scenario 5: Network Latency

**Setup**: Add artificial delay
Linux: Add 1000ms delay
sudo tc qdisc add dev lo root netem delay 1000ms

text

**Test**:
1. User borrows book
2. Observe Saga behavior with network delay

**Expected Results**:
- ✅ Saga completes despite delay
- ✅ No premature timeout
- ✅ User sees loading indicator

**Cleanup**:
sudo tc qdisc del dev lo root netem

text

---

## Scenario 6: Concurrent Borrow Same Book

**Setup**: Two users borrow last copy simultaneously

**Test**:
1. Book has AvailableCopies = 1
2. User A borrows book (start)
3. User B borrows same book (start immediately)
4. Check results

**Expected Results**:
- ✅ One loan status = "CheckedOut"
- ✅ Other loan status = "FAILED"
- ✅ AvailableCopies = 0 (not -1!)
- ✅ No race condition

**Verification**:
SELECT Status FROM loan_db.Loans WHERE BookId=<id> ORDER BY LoanId DESC LIMIT 2;
SELECT AvailableCopies FROM catalog_db.Books WHERE BookId=<id>;

text

---

## Scenario 7: Service Restart During Transaction

**Setup**: Restart LoanService during borrow

**Test**:
1. User initiates borrow
2. Kill LoanService mid-transaction
pkill -9 -f LoanService.Api

text
3. Restart service
4. Check loan status

**Expected Results**:
- ✅ Loan either completes or marked FAILED
- ✅ No orphaned PENDING loans
- ✅ Stock remains consistent

---

## Scenario 8: Memory Leak Detection

**Test**: Run system under load for extended period (1 hour)

**Monitor**:
Memory usage
ps aux | grep dotnet

Check for increasing memory
watch -n 30 'ps aux | grep dotnet'

text

**Expected Results**:
- ✅ Memory usage stabilizes
- ✅ No memory leaks detected
- ✅ Garbage collection working

---

## Failure Recovery Checklist

- [ ] Services restart automatically (systemd/docker)
- [ ] Database connections re-established after failure
- [ ] Saga compensating transactions execute
- [ ] No data corruption after failure
- [ ] Logs contain failure details for debugging
- [ ] Monitoring alerts triggered on failure
- [ ] Circuit breaker prevents cascading failures (if implemented)

---

## Chaos Engineering (Advanced)

**Tool**: Chaos Mesh or manual scripts

**Scenarios**:
- Random pod/service kills
- Network partition between services
- CPU/memory stress testing
- Disk I/O throttling

---

## Recovery Time Objectives

| Failure Type | Max Recovery Time | Actual | Status |
|--------------|-------------------|--------|--------|
| Service crash | 30 seconds | TBD | Pending |
| DB connection loss | 10 seconds | TBD | Pending |
| Network partition | 1 minute | TBD | Pending |

---

## Resilience Test Results

| Test | Result | Issues Found | Notes |
|------|--------|--------------|-------|
| CatalogService down | ✅ Pass | None | Saga compensating tx works |
| DB connection lost | ⚠️ Partial | Retry needed | Added connection retry |
| Gateway down | ✅ Pass | None | Frontend handles gracefully |