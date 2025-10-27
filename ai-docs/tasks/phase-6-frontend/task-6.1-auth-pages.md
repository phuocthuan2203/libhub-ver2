# Task 6.1: Implement Authentication Pages

**Phase**: 6 - Frontend Implementation  
**Estimated Time**: 2 hours  
**Dependencies**: Phase 5 complete (Gateway running)

---

## Objective

Build login and registration pages with JWT token management.

---

## Key Implementation Points

### 1. Project Structure

frontend/
├── login.html
├── register.html
├── index.html (redirect if not logged in)
├── css/styles.css
└── js/
├── api-client.js
└── auth.js

text

### 2. login.html

<form id="loginForm"> <input type="email" id="email" required> <input type="password" id="password" required> <button type="submit">Login</button> </form> <p>Don't have account? <a href="register.html">Register</a></p> <script> document.getElementById('loginForm').addEventListener('submit', async (e) => { e.preventDefault(); // POST to http://localhost:5000/api/users/login // Store token in localStorage // Extract role from JWT (decode payload) // Redirect to index.html }); </script>
text

### 3. register.html

<form id="registerForm"> <input type="text" id="username" required> <input type="email" id="email" required> <input type="password" id="password" required minlength="8"> <button type="submit">Register</button> </form> <script> // POST to /api/users/register // Auto-login after registration (call login endpoint) // Store token and redirect </script>
text

### 4. auth.js Utilities

// JWT token management
function saveToken(token) {
localStorage.setItem('jwt_token', token);
const payload = JSON.parse(atob(token.split('.')));​
localStorage.setItem('user_role', payload.role);
localStorage.setItem('user_id', payload.sub);
}

function isLoggedIn() {
return localStorage.getItem('jwt_token') !== null;
}

function requireAuth() {
if (!isLoggedIn()) {
window.location.href = 'login.html';
}
}

function logout() {
localStorage.clear();
window.location.href = 'login.html';
}

text

### 5. Password Validation

function validatePassword(password) {
const minLength = password.length >= 8;
const hasUpper = /[A-Z]/.test(password);
const hasLower = /[a-z]/.test(password);
const hasDigit = /[0-9]/.test(password);
const hasSpecial = /[^A-Za-z0-9]/.test(password);

text
return minLength && hasUpper && hasLower && hasDigit && hasSpecial;
}

text

---

## Acceptance Criteria

- [ ] Login page with email/password form
- [ ] Register page with validation
- [ ] JWT token stored in localStorage
- [ ] Role extracted and stored separately
- [ ] Redirect to index.html after successful login
- [ ] Error messages displayed for failed login/register
- [ ] Password validation on client side
- [ ] Works with Gateway at localhost:5000

---

## After Completion

Update **PROJECT_STATUS.md**:
✅ Task 6.1: Authentication Pages (date)

Login and registration forms

JWT token management

Client-side validation

text

**Next: Task 6.2** (Catalog pages)