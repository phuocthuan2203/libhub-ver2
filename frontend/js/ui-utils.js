let toastContainer = null;

function initToastContainer() {
    if (!toastContainer) {
        toastContainer = document.createElement('div');
        toastContainer.className = 'toast-container';
        document.body.appendChild(toastContainer);
    }
    return toastContainer;
}

function showToast(message, type = 'info', duration = 4000) {
    const container = initToastContainer();
    
    const icons = {
        success: '✓',
        error: '✕',
        warning: '⚠',
        info: 'ℹ'
    };
    
    const toast = document.createElement('div');
    toast.className = `toast ${type}`;
    
    toast.innerHTML = `
        <span class="toast-icon">${icons[type] || icons.info}</span>
        <div class="toast-content">
            <div class="toast-message">${message}</div>
        </div>
        <button class="toast-close" onclick="this.parentElement.remove()">×</button>
    `;
    
    container.appendChild(toast);
    
    if (duration > 0) {
        setTimeout(() => {
            toast.style.animation = 'slideIn 0.3s ease-out reverse';
            setTimeout(() => toast.remove(), 300);
        }, duration);
    }
    
    return toast;
}

function showLoadingOverlay(message = 'Loading...') {
    const overlay = document.createElement('div');
    overlay.className = 'loading-overlay';
    overlay.id = 'loadingOverlay';
    overlay.innerHTML = `
        <div style="text-align: center; color: white;">
            <div class="spinner spinner-large" style="margin: 0 auto 1rem;"></div>
            <div>${message}</div>
        </div>
    `;
    document.body.appendChild(overlay);
    return overlay;
}

function hideLoadingOverlay() {
    const overlay = document.getElementById('loadingOverlay');
    if (overlay) {
        overlay.remove();
    }
}

function setButtonLoading(button, isLoading, originalText = null) {
    if (isLoading) {
        button.dataset.originalText = button.textContent;
        button.disabled = true;
        button.classList.add('btn-loading');
    } else {
        button.disabled = false;
        button.classList.remove('btn-loading');
        if (originalText) {
            button.textContent = originalText;
        } else if (button.dataset.originalText) {
            button.textContent = button.dataset.originalText;
        }
    }
}

function debounce(func, wait) {
    let timeout;
    return function executedFunction(...args) {
        const later = () => {
            clearTimeout(timeout);
            func(...args);
        };
        clearTimeout(timeout);
        timeout = setTimeout(later, wait);
    };
}

function createSkeletonCard() {
    return `
        <div class="book-card">
            <div class="skeleton skeleton-title"></div>
            <div class="skeleton skeleton-text"></div>
            <div class="skeleton skeleton-text"></div>
            <div class="skeleton skeleton-text"></div>
            <div class="skeleton skeleton-text" style="width: 60%;"></div>
        </div>
    `;
}

function showSkeletonLoading(containerId, count = 6) {
    const container = document.getElementById(containerId);
    if (container) {
        container.innerHTML = Array(count).fill(createSkeletonCard()).join('');
    }
}

function handleApiError(error, defaultMessage = 'An error occurred') {
    let message = defaultMessage;
    
    if (error.message.includes('401')) {
        message = 'Session expired. Please login again.';
        setTimeout(() => {
            window.location.href = 'login.html';
        }, 2000);
    } else if (error.message.includes('403')) {
        message = 'You do not have permission to perform this action.';
    } else if (error.message.includes('404')) {
        message = 'Resource not found.';
    } else if (error.message.includes('500')) {
        message = 'Server error. Please try again later.';
    }
    
    showToast(message, 'error');
    console.error('API Error:', error);
}
