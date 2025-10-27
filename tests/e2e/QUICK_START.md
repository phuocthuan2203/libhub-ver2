# E2E Tests - Quick Start Guide

## 1. Setup (One-time)

```bash
cd tests/e2e
npm install
npx playwright install chromium
```

## 2. Populate Test Data

```bash
mysql -u libhub_user -p < test-data-setup.sql
```

Enter password: `libhub_password`

## 3. Start All Services

**Terminal 1 - UserService:**
```bash
cd src/Services/UserService/LibHub.UserService.Api
dotnet run
```

**Terminal 2 - CatalogService:**
```bash
cd src/Services/CatalogService/LibHub.CatalogService.Api
dotnet run
```

**Terminal 3 - LoanService:**
```bash
cd src/Services/LoanService/LibHub.LoanService.Api
dotnet run
```

**Terminal 4 - Gateway:**
```bash
cd src/Gateway/LibHub.Gateway.Api
dotnet run
```

**Terminal 5 - Frontend:**
```bash
cd frontend
python3 -m http.server 4200
```

## 4. Run Tests

```bash
cd tests/e2e
npm test
```

## 5. View Results

```bash
npm run report
```

## Test Credentials

- **Customer**: customer@test.com / Test@1234
- **Admin**: admin@test.com / Admin@1234

## Verify Database

```bash
mysql -u libhub_user -p
```

```sql
-- Check test users
SELECT * FROM user_db.Users WHERE Email LIKE '%@test.com';

-- Check test books
SELECT * FROM catalog_db.Books WHERE Isbn LIKE '9999%';

-- Check loans
SELECT * FROM loan_db.Loans;
```

## Cleanup Test Data

```sql
DELETE FROM loan_db.Loans WHERE UserId IN (
    SELECT UserId FROM user_db.Users WHERE Email LIKE '%@test.com'
);
DELETE FROM catalog_db.Books WHERE Isbn LIKE '9999%';
DELETE FROM user_db.Users WHERE Email LIKE '%@test.com';
```
