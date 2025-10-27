# Task 5.4: Gateway Integration Testing

**Phase**: 5 - API Gateway Implementation  
**Estimated Time**: 2 hours  
**Dependencies**: Task 5.3 (JWT middleware)

---

## Objective

Test complete integration of Gateway with all three services and verify end-to-end workflows.

---

## Test Scenarios

### 1. Public Endpoints (No Auth Required)

Search books
curl http://localhost:5000/api/books

Get book by ID
curl http://localhost:5000/api/books/1

Register new user
curl -X POST http://localhost:5000/api/users/register
-H "Content-Type: application/json"
-d '{"username":"testuser","email":"test@test.com","password":"Test123!"}'

text

### 2. Authentication Flow

Login and capture token
TOKEN=$(curl -X POST http://localhost:5000/api/users/login
-H "Content-Type: application/json"
-d '{"email":"test@test.com","password":"Test123!"}'
| jq -r '.accessToken')

echo $TOKEN

text

### 3. Protected Endpoints (Requires Auth)

Get user profile
curl http://localhost:5000/api/users/me
-H "Authorization: Bearer $TOKEN"

Borrow book (Saga workflow!)
curl -X POST http://localhost:5000/api/loans
-H "Authorization: Bearer $TOKEN"
-H "Content-Type: application/json"
-d '{"bookId":1}'

Get user loans
curl http://localhost:5000/api/loans/user/1
-H "Authorization: Bearer $TOKEN"

Return book
curl -X PUT http://localhost:5000/api/loans/1/return
-H "Authorization: Bearer $TOKEN"

text

### 4. Admin Operations (Requires Admin Role)

Create admin user first in UserService directly
Then login as admin and get admin token
Add book to catalog
curl -X POST http://localhost:5000/api/books
-H "Authorization: Bearer $ADMIN_TOKEN"
-H "Content-Type: application/json"
-d '{"isbn":"9781234567890","title":"New Book","author":"Author","genre":"Fiction","totalCopies":5}'

text

---

## End-to-End Workflow Test

**Complete User Journey**:
1. Register → 2. Login → 3. Search Books → 4. Borrow Book → 5. View Loans → 6. Return Book

Script to test complete workflow
./test-e2e.sh

text

---

## Verify Saga Pattern Through Gateway

Test distributed transaction
1. Check book availability
curl http://localhost:5000/api/books/1

2. Borrow book (triggers Saga)
curl -X POST http://localhost:5000/api/loans
-H "Authorization: Bearer $TOKEN"
-d '{"bookId":1}'

3. Verify stock decreased in CatalogService
curl http://localhost:5000/api/books/1

availableCopies should be decremented
4. Verify loan created in LoanService
curl http://localhost:5000/api/loans/user/1
-H "Authorization: Bearer $TOKEN"

text

---

## Common Issues

1. **401 Unauthorized**: JWT secret key mismatch between services and Gateway
2. **404 Not Found**: Service not running or wrong port in ocelot.json
3. **CORS Error**: Frontend can't call Gateway - check CORS config
4. **Timeout**: Service taking too long - check service health

---

## Acceptance Criteria

- [ ] All public endpoints accessible without token
- [ ] Login returns valid JWT token
- [ ] Protected endpoints require Bearer token
- [ ] Admin endpoints require Admin role
- [ ] Complete user journey works end-to-end
- [ ] Saga pattern works through Gateway (borrow → stock decremented)
- [ ] All three services accessible through Gateway
- [ ] No routing errors

---

## After Completion

Update **PROJECT_STATUS.md**:
Phase Status Overview
| Phase 5: API Gateway | ✅ COMPLETE | 100% (4/4) | All services integrated! |

Completed Tasks
✅ Task 5.4: Gateway Integration Testing (date)

End-to-end workflow tested

Saga pattern verified through Gateway

All services accessible via single entry point

Phase 5 Complete! 🎉

Service Readiness Status
| Gateway | N/A | N/A | N/A | ✅ | ✅ | ✅ | ✅ |

Integration Status
✅ UserService → Gateway routing

✅ CatalogService → Gateway routing

✅ LoanService → Gateway routing

✅ JWT validation at Gateway level

✅ Saga workflow through Gateway

Overall Progress
Overall Progress: 85% (17/20 tasks complete)

Next Steps
Phase 6: Frontend Implementation

Build user interfaces

Connect to Gateway API

Test complete application

text

**Next: Phase 6** - Frontend (final phase!)