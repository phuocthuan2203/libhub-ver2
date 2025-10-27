# LibHub - API Contracts Reference

Quick reference for all REST API endpoints across services. All requests through API Gateway at `http://localhost:5000`.

---

## UserService Endpoints

### POST /api/users/register
**Description**: Register a new user account  
**Authentication**: None (public)  
**Request Body**:
```
{
  "username": "string",
  "email": "string",
  "password": "string"
}
```
**Response 201**:
```
{
  "userId": 1,
  "username": "string",
  "email": "string",
  "role": "Customer"
}
```
**Errors**: 400 (email exists, validation failed)

---

### POST /api/users/login
**Description**: Authenticate user and receive JWT token  
**Authentication**: None (public)  
**Request Body**:
```
{
  "email": "string",
  "password": "string"
}
```
**Response 200**:
```
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "expiresIn": 3600
}
```
**Errors**: 401 (invalid credentials)

---

## CatalogService Endpoints

### GET /api/books
**Description**: Search/list books in catalog  
**Authentication**: None (public)  
**Query Parameters**:
- `search` (optional): Search term (title, author, ISBN)
- `genre` (optional): Filter by genre

**Response 200**:
```
[
  {
    "bookId": 1,
    "isbn": "9781234567890",
    "title": "Book Title",
    "author": "Author Name",
    "genre": "Fiction",
    "description": "Book description",
    "totalCopies": 5,
    "availableCopies": 3,
    "isAvailable": true
  }
]
```

---

### GET /api/books/{id}
**Description**: Get single book details  
**Authentication**: None (public)  
**Path Parameters**: `id` (integer)  
**Response 200**: Same as single book object above  
**Errors**: 404 (book not found)

---

### POST /api/books
**Description**: Add new book to catalog (admin only)  
**Authentication**: Required (Admin role)  
**Headers**: `Authorization: Bearer {token}`  
**Request Body**:
```
{
  "isbn": "9781234567890",
  "title": "string",
  "author": "string",
  "genre": "string",
  "description": "string",
  "totalCopies": 5
}
```
**Response 201**: Created book object  
**Errors**: 400 (ISBN exists), 401 (unauthorized), 403 (forbidden)

---

### PUT /api/books/{id}
**Description**: Update book details (admin only)  
**Authentication**: Required (Admin role)  
**Headers**: `Authorization: Bearer {token}`  
**Path Parameters**: `id` (integer)  
**Request Body**:
```
{
  "title": "string",
  "author": "string",
  "genre": "string",
  "description": "string"
}
```
**Response 204**: No content  
**Errors**: 404 (not found), 401, 403

---

### DELETE /api/books/{id}
**Description**: Delete book from catalog (admin only)  
**Authentication**: Required (Admin role)  
**Headers**: `Authorization: Bearer {token}`  
**Path Parameters**: `id` (integer)  
**Response 204**: No content  
**Errors**: 400 (book has active loans), 404, 401, 403

---

### PUT /api/books/{id}/stock
**Description**: Update book stock (internal - called by LoanService)  
**Authentication**: Required (any authenticated user)  
**Headers**: `Authorization: Bearer {token}`  
**Path Parameters**: `id` (integer)  
**Request Body**:
```
{
  "changeAmount": -1
}
```
**Values**: `-1` for decrement (borrow), `+1` for increment (return)  
**Response 204**: No content  
**Errors**: 400 (no stock available), 404, 401

---

## LoanService Endpoints

### POST /api/loans
**Description**: Borrow a book (customer)  
**Authentication**: Required (Customer or Admin)  
**Headers**: `Authorization: Bearer {token}`  
**Request Body**:
```
{
  "bookId": 1
}
```
**Note**: `userId` extracted from JWT token  
**Response 201**:
```
{
  "loanId": 1,
  "userId": 1,
  "bookId": 1,
  "status": "CheckedOut",
  "checkoutDate": "2025-10-27T08:00:00Z",
  "dueDate": "2025-11-10T08:00:00Z",
  "returnDate": null,
  "isOverdue": false,
  "daysUntilDue": 14
}
```
**Errors**: 400 (max loans reached, book unavailable), 401

---

### PUT /api/loans/{id}/return
**Description**: Return a borrowed book  
**Authentication**: Required (Customer or Admin)  
**Headers**: `Authorization: Bearer {token}`  
**Path Parameters**: `id` (integer, loanId)  
**Response 204**: No content  
**Errors**: 400 (loan not active), 404, 401

---

### GET /api/loans/user/{userId}
**Description**: Get loans for specific user  
**Authentication**: Required (own userId or Admin)  
**Headers**: `Authorization: Bearer {token}`  
**Path Parameters**: `userId` (integer)  
**Response 200**: Array of loan objects  
**Errors**: 403 (accessing other user's loans), 401

---

### GET /api/loans
**Description**: Get all active loans (admin only)  
**Authentication**: Required (Admin role)  
**Headers**: `Authorization: Bearer {token}`  
**Response 200**: Array of loan objects  
**Errors**: 401, 403

---

## Common Response Patterns

### Success Responses
- **200 OK**: Request successful, returning data
- **201 Created**: Resource created, returning created object
- **204 No Content**: Request successful, no data returned

### Error Responses
- **400 Bad Request**: Validation error, business rule violation
- **401 Unauthorized**: Missing or invalid JWT token
- **403 Forbidden**: Valid token but insufficient permissions
- **404 Not Found**: Resource doesn't exist
- **500 Internal Server Error**: Server-side error

### Error Body Format
```
{
  "message": "Error description",
  "errors": {
    "fieldName": ["Validation error message"]
  }
}
```

---

## Authentication Flow

1. **Register/Login** → Receive JWT token
2. **Store Token** → Save in localStorage/sessionStorage
3. **Attach to Requests** → Add `Authorization: Bearer {token}` header
4. **Token Expiry** → 1 hour, re-login required

---

## JWT Token Claims

```
{
  "sub": "1",                    // User ID
  "email": "user@example.com",   // User email
  "role": "Customer",            // Role (Customer or Admin)
  "exp": 1698414000,             // Expiration timestamp
  "iss": "LibHub.UserService",   // Issuer
  "aud": "LibHub.Clients"        // Audience
}
```

---

## Inter-Service Communication

### LoanService → CatalogService

**During Borrow (Saga)**:
1. `GET /api/books/{id}` - Check availability
2. `PUT /api/books/{id}/stock` with `{"changeAmount": -1}` - Decrement stock

**During Return**:
1. `PUT /api/books/{id}/stock` with `{"changeAmount": 1}` - Increment stock

**Note**: These are direct HTTP calls, not through Gateway

---

## Frontend API Usage Examples

### Register User
```
const response = await fetch('http://localhost:5000/api/users/register', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    username: 'John Doe',
    email: 'john@example.com',
    password: 'Password123!'
  })
});
```

### Login and Store Token
```
const response = await fetch('http://localhost:5000/api/users/login', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    email: 'john@example.com',
    password: 'Password123!'
  })
});
const data = await response.json();
localStorage.setItem('jwt_token', data.accessToken);
```

### Search Books (Public)
```
const response = await fetch('http://localhost:5000/api/books?search=fiction');
const books = await response.json();
```

### Borrow Book (Authenticated)
```
const token = localStorage.getItem('jwt_token');
const response = await fetch('http://localhost:5000/api/loans', {
  method: 'POST',
  headers: {
    'Content-Type': 'application/json',
    'Authorization': `Bearer ${token}`
  },
  body: JSON.stringify({ bookId: 1 })
});
```

