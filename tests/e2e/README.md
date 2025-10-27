# LibHub E2E Tests

End-to-End testing suite for the LibHub library management system using Playwright.

## Prerequisites

- All LibHub services running:
  - UserService (port 5002)
  - CatalogService (port 5001)
  - LoanService (port 5003)
  - API Gateway (port 5000)
  - Frontend (port 4200)
- MySQL databases accessible (user_db, catalog_db, loan_db)
- Node.js 16+ installed

## Setup

### 1. Install Dependencies

```bash
cd tests/e2e
npm install
npx playwright install chromium
```

### 2. Populate Test Data

Run the test data setup script to create test users and books:

```bash
mysql -u libhub_user -p < test-data-setup.sql
```

**Test Credentials:**
- Customer: `customer@test.com` / `Test@1234`
- Admin: `admin@test.com` / `Admin@1234`

### 3. Start All Services

Ensure all services are running before executing tests:

```bash
cd src/Services/UserService/LibHub.UserService.Api
dotnet run

cd src/Services/CatalogService/LibHub.CatalogService.Api
dotnet run

cd src/Services/LoanService/LibHub.LoanService.Api
dotnet run

cd src/Gateway/LibHub.Gateway.Api
dotnet run

cd frontend
python3 -m http.server 4200
```

## Running Tests

### Run All Tests

```bash
npm test
```

### Run Tests in Headed Mode (see browser)

```bash
npm run test:headed
```

### Debug Tests

```bash
npm run test:debug
```

### Run Tests with UI Mode

```bash
npm run test:ui
```

### View Test Report

```bash
npm run report
```

## Test Scenarios

### Scenario 1: Customer Happy Path ✅
**File:** `e2e-customer-journey.spec.js`

**Flow:**
1. Register new user
2. Login with credentials
3. Browse and search books
4. View book details
5. Borrow book (Saga transaction)
6. View loan in "My Loans"
7. Return book
8. Verify loan status updated

**Verifies:**
- User registration and authentication
- JWT token management
- Book catalog browsing
- Distributed Saga transaction (loan_db + catalog_db)
- Stock decrement on borrow
- Stock increment on return

### Scenario 2: Admin Workflow (TODO)
- Admin login
- Add new book
- Edit book details
- View all loans
- Delete book (with validation)

### Scenario 3: Saga Failure - Book Unavailable (TODO)
- Attempt to borrow book with 0 stock
- Verify compensating transaction
- Loan status = "FAILED"
- Stock remains unchanged

### Scenario 4: Max Loans Limit (TODO)
- Borrow 5 books
- Attempt 6th borrow
- Verify error handling

### Scenario 5: Overdue Loans (TODO)
- Set loan due date to past
- Verify overdue indicator in UI

## Database Verification

Use the queries in `db-verification-queries.sql` to manually verify test results:

```bash
mysql -u libhub_user -p < db-verification-queries.sql
```

**Key Queries:**
- Check loan status
- Verify stock changes
- Count active loans
- Find overdue loans
- Validate Saga integrity

## Test Data

The `test-data-setup.sql` script creates:

**Users:**
- 1 Customer user
- 1 Admin user

**Books:**
- 7 test books with varying stock levels
- ISBN pattern: `9999*` (easy to identify test data)
- Stock levels: 10, 5, 3, 1, 0, 4, 6 copies

## Cleanup

To remove all test data:

```sql
DELETE FROM loan_db.Loans WHERE UserId IN (
    SELECT UserId FROM user_db.Users WHERE Email LIKE '%@test.com'
);
DELETE FROM catalog_db.Books WHERE Isbn LIKE '9999%';
DELETE FROM user_db.Users WHERE Email LIKE '%@test.com';
```

## Troubleshooting

### Tests Fail with "Navigation timeout"
- Ensure frontend is running on port 4200
- Check that all backend services are running
- Verify database connections

### Database Connection Errors
- Check MySQL credentials in test files
- Ensure `libhub_user` has access to all databases
- Verify connection string in `e2e-customer-journey.spec.js`

### JWT Token Issues
- Clear browser localStorage before tests
- Verify JWT secret matches across all services
- Check token expiration (1 hour default)

### Saga Transaction Failures
- Check LoanService logs for Saga step failures
- Verify CatalogService is accessible from LoanService
- Run database verification queries to check data consistency

## CI/CD Integration

To run tests in CI pipeline:

```bash
CI=true npm test
```

This enables:
- 2 retries on failure
- Headless mode
- Parallel execution disabled (sequential for data consistency)

## Contributing

When adding new test scenarios:
1. Create new `.spec.js` file in this directory
2. Follow existing naming convention
3. Add database verification queries to `db-verification-queries.sql`
4. Update this README with scenario description
5. Ensure tests clean up after themselves

## Resources

- [Playwright Documentation](https://playwright.dev)
- [LibHub Architecture](../../ai-docs/master-context/00_PROJECT_CONTEXT.md)
- [E2E Test Scenarios](../../ai-docs/testing-phase/E2E_TEST_SCENARIOS.md)
