function saveToken(token) {
    localStorage.setItem('jwt_token', token);
    const payload = JSON.parse(atob(token.split('.')[1]));
    localStorage.setItem('user_role', payload.role || 'Customer');
    localStorage.setItem('user_id', payload.sub);
    localStorage.setItem('user_email', payload.email || '');
}

function isLoggedIn() {
    return localStorage.getItem('jwt_token') !== null;
}

function isAdmin() {
    const role = localStorage.getItem('user_role');
    return role === 'Admin';
}

function getUserId() {
    return localStorage.getItem('user_id');
}

function getUserRole() {
    return localStorage.getItem('user_role');
}

function requireAuth() {
    if (!isLoggedIn()) {
        alert('Please login to access this page');
        window.location.href = 'login.html';
    }
}

function requireAdmin() {
    if (!isLoggedIn()) {
        alert('Please login to access this page');
        window.location.href = 'login.html';
        return;
    }
    
    if (!isAdmin()) {
        alert('Access denied. Admin only.');
        window.location.href = 'index.html';
    }
}

function logout() {
    localStorage.clear();
    window.location.href = 'login.html';
}

function validatePassword(password) {
    const minLength = password.length >= 8;
    const hasUpper = /[A-Z]/.test(password);
    const hasLower = /[a-z]/.test(password);
    const hasDigit = /[0-9]/.test(password);
    const hasSpecial = /[^A-Za-z0-9]/.test(password);
    
    return minLength && hasUpper && hasLower && hasDigit && hasSpecial;
}

function getPasswordValidationMessage(password) {
    const errors = [];
    
    if (password.length < 8) {
        errors.push('at least 8 characters');
    }
    if (!/[A-Z]/.test(password)) {
        errors.push('one uppercase letter');
    }
    if (!/[a-z]/.test(password)) {
        errors.push('one lowercase letter');
    }
    if (!/[0-9]/.test(password)) {
        errors.push('one digit');
    }
    if (!/[^A-Za-z0-9]/.test(password)) {
        errors.push('one special character');
    }
    
    if (errors.length === 0) {
        return '';
    }
    
    return 'Password must contain: ' + errors.join(', ');
}

function updateNavigation() {
    const navLinks = document.getElementById('navLinks');
    if (!navLinks) return;
    
    if (isLoggedIn()) {
        const role = getUserRole();
        navLinks.innerHTML = `
            <a href="index.html">Browse Books</a>
            <a href="my-loans.html">My Loans</a>
            ${role === 'Admin' ? '<a href="admin-catalog.html">Admin Panel</a>' : ''}
            <button onclick="logout()" class="btn-logout">Logout</button>
        `;
    } else {
        navLinks.innerHTML = `
            <a href="index.html">Browse Books</a>
            <a href="login.html">Login</a>
            <a href="register.html">Register</a>
        `;
    }
}
