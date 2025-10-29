const API_BASE_URL = '/api';

class ApiClient {
    constructor(baseUrl) {
        this.baseUrl = baseUrl;
    }
    
    async get(endpoint, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, { 
            method: 'GET',
            headers 
        });
        
        if (!response.ok) {
            throw new Error(`HTTP ${response.status}: ${response.statusText}`);
        }
        
        return await response.json();
    }
    
    async post(endpoint, data, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'POST',
            headers,
            body: JSON.stringify(data)
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText || response.statusText}`);
        }
        
        return response.status === 204 ? null : await response.json();
    }
    
    async put(endpoint, data = null, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const options = {
            method: 'PUT',
            headers
        };
        
        if (data !== null) {
            options.body = JSON.stringify(data);
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, options);
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText || response.statusText}`);
        }
        
        return response.status === 204 ? null : await response.json();
    }
    
    async delete(endpoint, requiresAuth = false) {
        const headers = { 'Content-Type': 'application/json' };
        if (requiresAuth) {
            const token = localStorage.getItem('jwt_token');
            if (token) {
                headers['Authorization'] = `Bearer ${token}`;
            }
        }
        
        const response = await fetch(`${this.baseUrl}${endpoint}`, {
            method: 'DELETE',
            headers
        });
        
        if (!response.ok) {
            const errorText = await response.text();
            throw new Error(`HTTP ${response.status}: ${errorText || response.statusText}`);
        }
    }
}

const apiClient = new ApiClient(API_BASE_URL);
