# HTTPS Deployment Guide

## Problem Fixed
Changed API client from hardcoded `http://` to relative URLs (`/api`) to support HTTPS.

## Server Setup Steps

### 1. Copy nginx configuration to server
```bash
sudo cp nginx-production.conf /etc/nginx/conf.d/libhub.conf
```

### 2. Update SSL certificate paths (if different)
Edit `/etc/nginx/conf.d/libhub.conf` and update these lines if your SSL cert paths differ:
```
ssl_certificate /etc/letsencrypt/live/libhub.thuannp4.online/fullchain.pem;
ssl_certificate_key /etc/letsencrypt/live/libhub.thuannp4.online/privkey.pem;
```

### 3. Update backend proxy target (if needed)
If your backend is not on `localhost:5000`, update this line:
```
proxy_pass http://localhost:5000;
```

### 4. Test nginx configuration
```bash
sudo nginx -t
```

### 5. Reload nginx
```bash
sudo systemctl reload nginx
```

### 6. Deploy updated frontend
Copy the updated `frontend/js/api-client.js` to your server:
```bash
scp frontend/js/api-client.js user@server:/var/www/libhub/frontend/js/
```

Or rebuild and redeploy your entire frontend.

## Verification
1. Visit https://libhub.thuannp4.online
2. Open browser DevTools Console
3. Check that API requests go to `https://libhub.thuannp4.online/api/...` (not http://)
4. Verify no Mixed Content errors

## Notes
- HTTP (port 80) automatically redirects to HTTPS (port 443)
- All `/api/*` requests are proxied to backend at `localhost:5000`
- SSL certificates should be renewed automatically if using Let's Encrypt
