# Task 7.2: API Contract Testing

**Phase**: 7 - System Integration & Testing  
**Type**: Manual + Postman  
**Estimated Time**: 3-4 hours  
**Dependencies**: Task 7.1 complete

---

## Objective

Verify all API endpoints match specifications and return correct responses.

---

## Tool Setup

**Option 1**: Postman (GUI)  
**Option 2**: cURL (command line)  
**Option 3**: AI-generated test collection

---

## Endpoints to Test

### UserService (3 endpoints)
- POST /api/users/register
- POST /api/users/login
- GET /api/users/me (auth required)

### CatalogService (5 endpoints)
- GET /api/books
- GET /api/books?search=fiction
- GET /api/books/{id}
- POST /api/books (admin only)
- PUT /api/books/{id}/stock (internal)

### LoanService (3 endpoints)
- POST /api/loans
- GET /api/loans/user/{userId}
- PUT /api/loans/{id}/return

**Total**: 11 endpoints

---

## Test Categories

### Happy Path Tests
- Verify 200/201 status codes
- Verify response body structure
- Verify required fields present

### Error Tests
- 401 Unauthorized (no token)
- 403 Forbidden (wrong role)
- 404 Not Found (invalid ID)
- 400 Bad Request (validation errors)

### Edge Cases
- Empty search results
- Borrow unavailable book
- Return already returned book

---

## AI Agent Task

**Generate Postman collection**:

**Prompt**:
Based on API_CONTRACTS.md, generate a complete Postman collection
for all 11 endpoints. Include authentication tests, error cases,
text

**Import to Postman** → **Run Collection**

---

## Sample Tests

Register
curl -X POST http://localhost:5000/api/users/register
-H "Content-Type: application/json"
-d '{"username":"test","email":"test@test.com","password":"Test123!"}'

Expected: 201 Created
Login
curl -X POST http://localhost:5000/api/users/login
-H "Content-Type: application/json"
-d '{"email":"test@test.com","password":"Test123!"}'

Expected: 200 OK, returns JWT token
Protected endpoint without auth
curl http://localhost:5000/api/users/me

Expected: 401 Unauthorized
text

---

## Contract Validation Checklist

- [ ] All endpoints return correct status codes
- [ ] Response bodies match OpenAPI specification
- [ ] Error responses follow standard format `{"message":"..."}`
- [ ] JWT tokens accepted across all services
- [ ] Admin endpoints reject Customer tokens
- [ ] Public endpoints work without authentication

---

## Acceptance Criteria

- [ ] All 11 endpoints tested
- [ ] All happy path tests pass
- [ ] All error cases handled correctly
- [ ] Postman collection created (if using)
- [ ] Results documented

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 7.2: API Contract Testing (date)

11/11 endpoints verified

All contracts match specification

Postman collection created

text

**Next**: Task 7.3 (Performance Testing)