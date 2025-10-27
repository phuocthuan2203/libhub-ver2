# Task 7.1: End-to-End Testing

**Phase**: 7 - System Integration & Testing  
**Type**: Manual Testing  
**Estimated Time**: 4-6 hours  
**Dependencies**: Phase 6 complete (Frontend working)

---

## Objective

Validate complete user journeys through the entire LibHub system including distributed transactions.

---

## Prerequisites

- [ ] All services running (5002, 5001, 5003, 5000)
- [ ] Frontend accessible
- [ ] Test data populated (see E2E_TEST_SCENARIOS.md)

---

## Test Scenarios to Execute

### 1. Customer Happy Path
- Register → Login → Search → Borrow → View Loans → Return
- **Verify**: Loan created in loan_db, stock decremented in catalog_db

### 2. Admin Workflow
- Login as admin → Add Book → Edit Book → View All Loans → Delete Book
- **Verify**: RBAC enforced, CRUD operations work

### 3. Saga Pattern - Book Unavailable
- Borrow book with 0 stock
- **Verify**: Loan status = "FAILED", stock not decremented

### 4. Max Loans Limit
- Borrow 5 books → Attempt 6th
- **Verify**: Error message, no 6th loan created

### 5. Overdue Loans
- Set loan due date to past
- **Verify**: Red highlight, "OVERDUE!" message

---

## AI Agent Task (Optional)

**Generate Playwright test scripts** for automated execution:

**Prompt**:
Read E2E_TEST_SCENARIOS.md and generate Playwright test script
for Scenario

text

**Execute**: `npx playwright test`

---

## Verification Queries

-- Check loan status
SELECT Status FROM loan_d<id>;

-- Check stock change
SELECT AvailableCopies FROM catalog_<id>;

-- Count active loans
SELECT COUNT(*) FROM loan_<id> AND Status='CheckedOut';

text

---

## Bug Tracking

Document all issues in PROJECT_STATUS.md:

| Bug ID | Scenario | Description | Severity | Status |
|--------|----------|-------------|----------|--------|
| E2E-001 | Borrow | Stock not decremented | Critical | Open |

---

## Acceptance Criteria

- [ ] All 5 scenarios executed successfully
- [ ] Saga distributed transaction verified
- [ ] No critical bugs remaining
- [ ] All results documented

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 7.1: End-to-End Testing (date)

5/5 scenarios passed

Saga pattern verified working

X bugs found, Y fixed

text

**Next**: Task 7.2 (API Contract Testing)