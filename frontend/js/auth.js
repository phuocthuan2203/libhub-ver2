function saveToken(token) {
    localStorage.setItem('jwt_token', token);
    const payload = JSON.parse(atob(token.split('.')[1]));
    localStorage.setItem('user_role', payload.role || 'Customer');
    localStorage.setItem('user_id', payload.sub);
    localStorage.setItem('user_email', payload.email || '');
    localStorage.setItem('username', payload.unique_name || payload.email || 'User');
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

function getUsername() {
    return localStorage.getItem('username') || 'User';
}

function initTheme() {
    const savedTheme = localStorage.getItem('theme') || 'light';
    document.documentElement.setAttribute('data-theme', savedTheme);
}

function toggleTheme() {
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const newTheme = currentTheme === 'light' ? 'dark' : 'light';
    document.documentElement.setAttribute('data-theme', newTheme);
    localStorage.setItem('theme', newTheme);
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
    const theme = localStorage.getItem('theme');
    localStorage.clear();
    if (theme) {
        localStorage.setItem('theme', theme);
    }
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
    initTheme();
    
    const nav = document.querySelector('nav');
    if (!nav) return;
    
    const currentTheme = document.documentElement.getAttribute('data-theme') || 'light';
    const themeIcon = currentTheme === 'light' ? '🌙' : '☀️';
    
    if (isLoggedIn()) {
        const role = getUserRole();
        const username = getUsername();
        nav.innerHTML = `
            <div class="nav-left">
                <h1 onclick="window.location.href='index.html'">LibHub</h1>
                <div class="nav-center">
                    <a href="index.html">Browse Books</a>
                    <a href="my-loans.html">My Loans</a>
                    ${role === 'Admin' ? '<a href="admin-catalog.html">Admin Panel</a>' : ''}
                </div>
            </div>
            <div class="nav-right">
                <span class="welcome-message">Welcome, <strong>${username}</strong></span>
                <button onclick="toggleTheme()" class="theme-toggle" title="Toggle theme">${themeIcon}</button>
                <button onclick="logout()" class="btn-logout">Logout</button>
            </div>
        `;
    } else {
        nav.innerHTML = `
            <div class="nav-left">
                <h1 onclick="window.location.href='index.html'">LibHub</h1>
                <div class="nav-center">
                    <a href="index.html">Browse Books</a>
                    <a href="login.html">Login</a>
                    <a href="register.html">Register</a>
                </div>
            </div>
            <div class="nav-right">
                <button onclick="toggleTheme()" class="theme-toggle" title="Toggle theme">${themeIcon}</button>
            </div>
        `;
    }
}
