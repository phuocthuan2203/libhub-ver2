# Task 7.5: Resilience Testing

**Phase**: 7 - System Integration & Testing  
**Type**: Failure Scenario Testing  
**Estimated Time**: 3-4 hours  
**Dependencies**: Task 7.4 complete

---

## Objective

Verify system handles failures gracefully and recovers correctly.

---

## Test Scenarios

### Scenario 1: CatalogService Down During Borrow ⚠️ CRITICAL

**Setup**:
pkill -f CatalogService.Api

text

**Test**: User attempts to borrow book

**Expected**:
- ✅ Loan status = "FAILED" (compensating transaction)
- ✅ User sees error message
- ✅ No "CheckedOut" loan created

**Verify**:
SELECT Status FROM loan_db.Loans WHERE LoanId=<id>;
-- Should be "FAILED"

text

**Priority**: CRITICAL (validates Saga pattern)

---

### Scenario 2: Database Connection Lost

**Setup**:
REVOKE ALL ON catalog_db.* FROM 'libhub_user'@'localhost';

text

**Test**: User searches books

**Expected**:
- ✅ 500 Internal Server Error returned
- ✅ System doesn't crash
- ✅ Error logged

**Cleanup**:
GRANT ALL ON catalog_db.* TO 'libhub_user'@'localhost';

text

---

### Scenario 3: Concurrent Borrow Same Book

**Setup**: Book with AvailableCopies = 1

**Test**: Two users borrow simultaneously

**Expected**:
- ✅ One loan "CheckedOut"
- ✅ Other loan "FAILED"
- ✅ AvailableCopies = 0 (NOT -1!)

**Verify**:
SELECT Status FROM loan_db.Loans WHERE BookId=<id> ORDER BY LoanId DESC LIMIT 2;
SELECT AvailableCopies FROM catalog_db.Books WHERE BookId=<id>;

text

**Priority**: CRITICAL (tests race condition handling)

---

### Scenario 4: Gateway Down

**Setup**:
pkill -f Gateway.Api

text

**Test**: Frontend calls API

**Expected**:
- ✅ Frontend shows connection error
- ✅ No JavaScript errors
- ✅ Graceful degradation

---

### Scenario 5: Network Latency

**Setup** (Linux):
sudo tc qdisc add dev lo root netem delay 1000ms

text

**Test**: User borrows book with 1-second delay

**Expected**:
- ✅ Saga completes despite delay
- ✅ No premature timeout
- ✅ User sees loading indicator

**Cleanup**:
sudo tc qdisc del dev lo root netem

text

---

### Scenario 6: Service Restart During Transaction

**Setup**: Start borrow, then kill LoanService mid-transaction

**Test**:
User starts borrow
pkill -9 -f LoanService.Api # Kill immediately

Restart service
text

**Expected**:
- ✅ Loan either completes or marked FAILED
- ✅ No orphaned PENDING loans
- ✅ Stock consistent

---

## Resilience Checklist

- [ ] Saga compensating transactions work
- [ ] Services handle downstream failures
- [ ] Database connection failures logged
- [ ] No data corruption after failures
- [ ] No race conditions in concurrent operations
- [ ] System recovers automatically after restart

---

## Recovery Time Measurement

| Failure Type | Max Recovery Time | Actual | Status |
|--------------|-------------------|--------|--------|
| Service crash | 30 seconds | ___ | Pending |
| DB connection lost | 10 seconds | ___ | Pending |
| Network partition | 1 minute | ___ | Pending |

---

## AI Agent Task

**Generate chaos testing script**:

**Prompt**:
Generate bash script to automate resilience testing scenarios.
Include random service kills, network delays, and database failures.

text

---

## Acceptance Criteria

- [ ] All 6 scenarios executed
- [ ] Saga compensating transactions verified
- [ ] No data corruption detected
- [ ] System recovers from all failures
- [ ] All results documented

---

## After Completion

Update **PROJECT_STATUS.md**:
Phase Status Overview
| Phase 7: Testing | ✅ COMPLETE | 100% (5/5) | All testing passed! |

Completed Tasks
✅ Task 7.5: Resilience Testing (date)

All failure scenarios tested

Saga pattern resilient

System recovers correctly

Phase 7 Complete! 🎉

🚀 LibHub READY FOR DEPLOYMENT! 🚀

Overall Progress
Overall Progress: 100% (25/25 tasks complete)

Final Testing Summary
✅ E2E Testing: 5/5 scenarios passed

✅ API Contract: 11/11 endpoints verified

✅ Performance: 200 users, <2s response

✅ Security: 0 critical vulnerabilities

✅ Resilience: Saga pattern working

Status: PRODUCTION READY ✅

text

**Git commit** → **Deploy to production!** 🚀