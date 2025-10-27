# LibHub - Frontend Specifications

**Technology**: Vanilla JavaScript (ES6+), HTML5, CSS3  
**Location**: `~/Projects/LibHub/frontend/`  
**Gateway URL**: `http://localhost:5000`

---

## Project Structure

```
frontend/
├── index.html              # Landing/Book Catalog page
├── login.html              # Login page
├── register.html           # Registration page
├── book-detail.html        # Individual book details
├── my-loans.html           # User's loan history
├── admin-catalog.html      # Admin book management
├── admin-add-book.html     # Admin add book form
├── admin-edit-book.html    # Admin edit book form
├── admin-loans.html        # Admin view all loans
├── css/
│   └── styles.css          # Global styles
└── js/
    ├── api-client.js       # Reusable API helper
    ├── auth.js             # Authentication utilities
    └── app.js              # Page-specific logic
```

---

## Page Specifications

### 1. Login Page (`login.html`)

**Purpose**: Authenticate existing users

**UI Elements**:
- Email input (text, required)
- Password input (password, required)
- "Login" button
- "Don't have an account? Register" link → `register.html`

**API Call**:
- **Endpoint**: `POST /api/users/login`
- **Body**: `{ "email": "...", "password": "..." }`
- **On Success**: Store JWT token, redirect to `index.html`
- **On Failure**: Display error message

**JavaScript**:
```
document.getElementById('loginForm').addEventListener('submit', async (e) => {
    e.preventDefault();
    const email = document.getElementById('email').value;
    const password = document.getElementById('password').value;
    
    try {
        const response = await fetch('http://localhost:5000/api/users/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password })
        });
        
        if (response.ok) {
            const data = await response.json();
            localStorage.setItem('jwt_token', data.accessToken);
            localStorage.setItem('user_role', extractRoleFromToken(data.accessToken));
            window.location.href = 'index.html';
        } else {
            alert('Invalid credentials');
        }
    } catch (error) {
        alert('Login failed');
    }
});
```

---

### 2. Registration Page (`register.html`)

**Purpose**: Create new user account

**UI Elements**:
- Username input (text, required)
- Email input (email, required)
- Password input (password, required, min 8 chars)
- "Register" button
- "Already have account? Login" link → `login.html`

**API Call**:
- **Endpoint**: `POST /api/users/register`
- **Body**: `{ "username": "...", "email": "...", "password": "..." }`
- **On Success**: Auto-login, store token, redirect to `index.html`
- **On Failure**: Display validation errors

---

### 3. Book Catalog Page (`index.html`)

**Purpose**: Browse and search books (public access)

**UI Elements**:
- Navigation bar with "My Loans" link (if logged in)
- Search input (text)
- Genre filter dropdown (optional)
- "Search" button
- Book grid/list displaying:
  - Book cover (placeholder image)
  - Title
  - Author
  - Genre
  - Available copies
  - "View Details" button → `book-detail.html?id={bookId}`

**API Call**:
- **Endpoint**: `GET /api/books?search={term}&genre={genre}`
- **Authentication**: Not required
- **On Load**: Display all books
- **On Search**: Filter and display results

**JavaScript**:
```
async function loadBooks(searchTerm = '', genre = '') {
    const query = new URLSearchParams({ search: searchTerm, genre });
    const response = await fetch(`http://localhost:5000/api/books?${query}`);
    const books = await response.json();
    
    const container = document.getElementById('bookGrid');
    container.innerHTML = books.map(book => `
        <div class="book-card">
            <h3>${book.title}</h3>
            <p>By ${book.author}</p>
            <p>Genre: ${book.genre}</p>
            <p>${book.isAvailable ? 'Available' : 'Not Available'} (${book.availableCopies}/${book.totalCopies})</p>
            <button onclick="viewDetails(${book.bookId})">View Details</button>
        </div>
    `).join('');
}
```

---

### 4. Book Detail Page (`book-detail.html`)

**Purpose**: Show book details and allow borrowing

**UI Elements**:
- Book title
- Author
- Genre
- Description
- Total copies / Available copies
- "Borrow This Book" button (if logged in and available)
- "Back to Catalog" link

**API Calls**:
- **Load Book**: `GET /api/books/{id}` (no auth)
- **Borrow Book**: `POST /api/loans` with `{ "bookId": {id} }` (requires auth)

**On Borrow Success**: Redirect to `my-loans.html`

---

### 5. My Loans Page (`my-loans.html`)

**Purpose**: Display user's borrowing history

**Authentication**: Required (Customer or Admin)

**UI Elements**:
- Navigation bar
- Table/list of loans:
  - Book title (link to book detail)
  - Checkout date
  - Due date
  - Status (CheckedOut, Returned, Overdue)
  - Days until due / Days overdue
  - "Return" button (if status = CheckedOut)

**API Call**:
- **Endpoint**: `GET /api/loans/user/{userId}` (userId from JWT)
- **Authentication**: Required
- **Return Book**: `PUT /api/loans/{loanId}/return`

**JavaScript**:
```
async function loadMyLoans() {
    const token = localStorage.getItem('jwt_token');
    const userId = extractUserIdFromToken(token);
    
    const response = await fetch(`http://localhost:5000/api/loans/user/${userId}`, {
        headers: { 'Authorization': `Bearer ${token}` }
    });
    
    const loans = await response.json();
    renderLoans(loans);
}

async function returnBook(loanId) {
    const token = localStorage.getItem('jwt_token');
    await fetch(`http://localhost:5000/api/loans/${loanId}/return`, {
        method: 'PUT',
        headers: { 'Authorization': `Bearer ${token}` }
    });
    loadMyLoans(); // Refresh
}
```

---

### 6. Admin Catalog Page (`admin-catalog.html`)

**Purpose**: Manage book catalog (Admin only)

**Authentication**: Required (Admin role)

**UI Elements**:
- Navigation bar with "Add New Book" button → `admin-add-book.html`
- Book table with columns:
  - ISBN
  - Title
  - Author
  - Genre
  - Total Copies
  - Available Copies
  - Actions: "Edit" button → `admin-edit-book.html?id={id}`, "Delete" button

**API Calls**:
- **Load Books**: `GET /api/books` (no auth, but page protected)
- **Delete Book**: `DELETE /api/books/{id}` (requires Admin)

---

### 7. Admin Add Book Page (`admin-add-book.html`)

**Purpose**: Add new book to catalog (Admin only)

**Authentication**: Required (Admin role)

**UI Elements**:
- Form with fields:
  - ISBN (text, 13 digits, required)
  - Title (text, required)
  - Author (text, required)
  - Genre (text or dropdown, required)
  - Description (textarea, optional)
  - Total Copies (number, required, min 0)
- "Add Book" button
- "Cancel" link → `admin-catalog.html`

**API Call**:
- **Endpoint**: `POST /api/books`
- **Body**: All form fields
- **Authentication**: Required (Admin)
- **On Success**: Redirect to `admin-catalog.html`

---

### 8. Admin Edit Book Page (`admin-edit-book.html`)

**Purpose**: Update book details (Admin only)

**Authentication**: Required (Admin role)

**UI Elements**:
- Same form as Add Book, but pre-filled
- "Update Book" button
- "Cancel" link

**API Calls**:
- **Load Book**: `GET /api/books/{id}` (populate form)
- **Update Book**: `PUT /api/books/{id}` (submit changes)

---

### 9. Admin Loans Page (`admin-loans.html`)

**Purpose**: View all system loans (Admin only)

**Authentication**: Required (Admin role)

**UI Elements**:
- Navigation bar
- Table of all loans:
  - Loan ID
  - User email/username
  - Book title
  - Checkout date
  - Due date
  - Status
  - Overdue indicator

**API Call**:
- **Endpoint**: `GET /api/loans` (Admin only)
- **Authentication**: Required (Admin)

---

## Shared JavaScript Utilities

### API Client (`js/api-client.js`)

```
class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }
    
    async get(endpoint, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, { headers });
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        return await response.json();
    }
    
    async post(endpoint, data, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers,
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        return response.status === 204 ? null : await response.json();
    }
    
    async put(endpoint, data, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'PUT',
            headers,
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
        return response.status === 204 ? null : await response.json();
    }
    
    async delete(endpoint, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            headers['Authorization'] = `Bearer ${token}`;
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'DELETE',
            headers
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}`);
        }
    }
}

const apiClient = new ApiClient('http://localhost:5000');
```

---

### Auth Utilities (`js/auth.js`)

```
function isLoggedIn() {
    return localStorage.getItem('jwt_token') !== null;
}

function isAdmin() {
    const role = localStorage.getItem('user_role');
    return role === 'Admin';
}

function extractUserIdFromToken(token) {
    const payload = JSON.parse(atob(token.split('.')));[1]
    return payload.sub;
}

function extractRoleFromToken(token) {
    const payload = JSON.parse(atob(token.split('.')));[1]
    return payload.role;
}

function logout() {
    localStorage.removeItem('jwt_token');
    localStorage.removeItem('user_role');
    window.location.href = 'login.html';
}

function requireAuth() {
    if (!isLoggedIn()) {
        window.location.href = 'login.html';
    }
}

function requireAdmin() {
    if (!isLoggedIn() || !isAdmin()) {
        alert('Access denied');
        window.location.href = 'index.html';
    }
}
```

---

## CSS Styling Guidelines

### Basic Structure

```
/* Global styles */
* {
    margin: 0;
    padding: 0;
    box-sizing: border-box;
}

body {
    font-family: Arial, sans-serif;
    line-height: 1.6;
    color: #333;
}

/* Navigation bar */
nav {
    background: #2c3e50;
    color: white;
    padding: 1rem;
}

/* Forms */
form {
    max-width: 500px;
    margin: 2rem auto;
    padding: 2rem;
    border: 1px solid #ddd;
    border-radius: 5px;
}

input, textarea, select {
    width: 100%;
    padding: 0.5rem;
    margin-bottom: 1rem;
}

button {
    background: #3498db;
    color: white;
    padding: 0.75rem 1.5rem;
    border: none;
    border-radius: 5px;
    cursor: pointer;
}

button:hover {
    background: #2980b9;
}

/* Book grid */
.book-grid {
    display: grid;
    grid-template-columns: repeat(auto-fill, minmax(250px, 1fr));
    gap: 1rem;
    padding: 2rem;
}

.book-card {
    border: 1px solid #ddd;
    padding: 1rem;
    border-radius: 5px;
}
```

---

## Key Implementation Notes

1. **No Framework**: Keep it simple with vanilla JavaScript
2. **JWT Storage**: Use localStorage for token storage
3. **Token Expiry**: Handle 401 errors by redirecting to login
4. **Role-Based UI**: Hide/show elements based on user role
5. **Error Handling**: Display user-friendly error messages
6. **Form Validation**: Client-side validation before API calls
7. **Loading States**: Show loading indicators during API calls
8. **Responsive Design**: Mobile-friendly layouts (optional)
9. **Security**: Never log tokens to console
10. **Navigation**: Consistent navigation bar across all pages
