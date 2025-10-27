# API Contract Testing

**Tool**: Postman / cURL  
**Base URL**: http://localhost:5000 (Gateway)

---

## UserService Endpoints

### POST /api/users/register
curl -X POST http://localhost:5000/api/users/register
-H "Content-Type: application/json"
-d '{"username":"test","email":"test@test.com","password":"Test123!"}'

text
**Expected**: 201 Created, returns `{"userId":1, "username":"test", "email":"test@test.com", "role":"Customer"}`

---

### POST /api/users/login
curl -X POST http://localhost:5000/api/users/login
-H "Content-Type: application/json"
-d '{"email":"test@test.com","password":"Test123!"}'

text
**Expected**: 200 OK, returns `{"accessToken":"eyJ...", "expiresIn":3600}`

---

### GET /api/users/me (requires auth)
curl http://localhost:5000/api/users/me
-H "Authorization: Bearer <TOKEN>"

text
**Expected**: 200 OK, returns user profile

---

## CatalogService Endpoints

### GET /api/books
curl http://localhost:5000/api/books

text
**Expected**: 200 OK, returns array of books

---

### GET /api/books?search=fiction
curl "http://localhost:5000/api/books?search=fiction"

text
**Expected**: 200 OK, returns filtered books

---

### GET /api/books/{id}
curl http://localhost:5000/api/books/1

text
**Expected**: 200 OK, returns single book

---

### POST /api/books (Admin only)
curl -X POST http://localhost:5000/api/books
-H "Authorization: Bearer <ADMIN_TOKEN>"
-H "Content-Type: application/json"
-d '{"isbn":"9783333333333","title":"New Book","author":"Author","genre":"Fiction","totalCopies":5}'

text
**Expected**: 201 Created, returns created book

---

### PUT /api/books/{id}/stock (Internal - for Saga)
curl -X PUT http://localhost:5000/api/books/1/stock
-H "Authorization: Bearer <TOKEN>"
-H "Content-Type: application/json"
-d '{"changeAmount":-1}'

text
**Expected**: 204 No Content

---

## LoanService Endpoints

### POST /api/loans (Borrow book)
curl -X POST http://localhost:5000/api/loans
-H "Authorization: Bearer <TOKEN>"
-H "Content-Type: application/json"
-d '{"bookId":1}'

text
**Expected**: 201 Created, returns loan object

---

### GET /api/loans/user/{userId}
curl http://localhost:5000/api/loans/user/1
-H "Authorization: Bearer <TOKEN>"

text
**Expected**: 200 OK, returns user's loans

---

### PUT /api/loans/{id}/return
curl -X PUT http://localhost:5000/api/loans/1/return
-H "Authorization: Bearer <TOKEN>"

text
**Expected**: 204 No Content

---

## Error Cases to Test

### 401 Unauthorized
curl http://localhost:5000/api/users/me

No Authorization header
text
**Expected**: 401 Unauthorized

---

### 403 Forbidden
curl -X POST http://localhost:5000/api/books
-H "Authorization: Bearer <CUSTOMER_TOKEN>"

Customer trying admin endpoint
text
**Expected**: 403 Forbidden

---

### 404 Not Found
curl http://localhost:5000/api/books/99999

text
**Expected**: 404 Not Found

---

### 400 Bad Request
curl -X POST http://localhost:5000/api/users/register
-H "Content-Type: application/json"
-d '{"username":"","email":"invalid","password":"123"}'

text
**Expected**: 400 Bad Request with validation errors

---

## Contract Validation Checklist

- [ ] All endpoints respond with correct status codes
- [ ] Response bodies match OpenAPI specification
- [ ] Required fields present in responses
- [ ] Error responses follow standard format `{"message":"..."}`
- [ ] JWT tokens work across all services
- [ ] Admin endpoints reject Customer tokens
- [ ] Public endpoints work without authentication