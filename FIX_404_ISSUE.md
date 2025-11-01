# Fix for 404 Error When Accessing API via External IP

## Problem
When accessing `http://192.168.1.4:8080` in the browser, API calls to `/api/books` return **404 Not Found**.

## Root Cause
The nginx configuration had `server_name localhost` which may cause issues when requests come from external IP addresses with different Host headers.

## Solution Applied

### Changes to `frontend/nginx.conf`:

1. **Changed `server_name localhost` to `server_name _`**
   - The underscore `_` is a special nginx value that matches ANY hostname
   - This allows requests from 192.168.1.4, localhost, domain names, etc.

2. **Updated `proxy_pass` to include the full path**
   - Changed from: `proxy_pass http://gateway:5000;`
   - Changed to: `proxy_pass http://gateway:5000/api/;`
   - This ensures the `/api/` path is properly forwarded

3. **Improved proxy headers**
   - Removed WebSocket-specific headers (Upgrade, Connection 'upgrade')
   - Added proper forwarding headers (X-Real-IP, X-Forwarded-For, X-Forwarded-Proto)
   - Disabled proxy buffering for better real-time response

## How to Apply on Server

### Step 1: Pull the latest changes
```bash
cd ~/LibHub
git pull origin main
```

### Step 2: Rebuild and restart the frontend container
```bash
docker compose up -d --build frontend
```

### Step 3: Clear browser cache
The browser may have cached the 404 response. You MUST clear the cache:

**Option A: Hard Refresh**
- Chrome/Firefox on Linux: `Ctrl + Shift + R`
- Chrome/Firefox on Mac: `Cmd + Shift + R`

**Option B: Clear Cache Completely**
- Chrome: Settings → Privacy and security → Clear browsing data → Cached images and files
- Firefox: Settings → Privacy & Security → Cookies and Site Data → Clear Data

**Option C: Use Incognito/Private Window**
- This ensures no cached responses are used

### Step 4: Test the API
```bash
curl http://192.168.1.4:8080/api/books
```

You should see JSON data with books.

## Verification Steps

1. **Test API from command line:**
```bash
curl -I http://192.168.1.4:8080/api/books
```
Should return: `HTTP/1.1 200 OK`

2. **Check nginx logs:**
```bash
docker logs libhub-frontend --tail 20
```
Should show successful 200 responses for `/api/books`

3. **Verify nginx configuration:**
```bash
docker exec libhub-frontend cat /etc/nginx/conf.d/default.conf
```
Should show `server_name _;` and `proxy_pass http://gateway:5000/api/;`

4. **Test from inside container:**
```bash
docker exec libhub-frontend wget -qO- http://127.0.0.1:8080/api/books
```
Should return JSON book data

## If Still Getting 404

### Check 1: Browser Cache
The most common cause is browser caching. Try:
1. Open DevTools (F12)
2. Go to Network tab
3. Check "Disable cache" checkbox
4. Refresh the page (F5)

### Check 2: Verify Container is Running
```bash
docker compose ps frontend
```
Should show STATUS as "Up"

### Check 3: Check Nginx Error Logs
```bash
docker logs libhub-frontend --tail 50 | grep error
```

### Check 4: Test Gateway Directly
```bash
curl http://192.168.1.4:5000/api/books
```
This should work and return book data.

### Check 5: Restart All Services
```bash
cd ~/LibHub
docker compose restart
```

### Check 6: Nuclear Option - Rebuild Everything
```bash
cd ~/LibHub
docker compose down
docker compose up -d --build
```

## Technical Details

### Why `server_name _` Works
- Nginx uses `server_name` to match incoming requests
- When set to `localhost`, it only matches requests with `Host: localhost`
- When accessing via `192.168.1.4:8080`, the Host header is `192.168.1.4:8080`
- The underscore `_` is a catch-all that matches ANY hostname

### Why `/api/` in proxy_pass
- Without trailing slash: `proxy_pass http://gateway:5000;`
  - Request to `/api/books` → Forwarded as `/api/books`
- With trailing slash and path: `proxy_pass http://gateway:5000/api/;`
  - Request to `/api/books` → Forwarded as `/api/books`
  - More explicit and clear about the intent

## Files Changed
- `frontend/nginx.conf` - Updated nginx reverse proxy configuration

## Commit
```
Fix nginx proxy for external access - accept all hostnames and improve proxy headers
```

## Next Steps After Fix
1. Test all pages: index.html, book-detail.html, my-loans.html, admin pages
2. Test login and registration
3. Test borrowing and returning books
4. Verify all API calls work correctly

## Support
If the issue persists after trying all troubleshooting steps:
1. Check firewall settings: `sudo firewall-cmd --list-all`
2. Check if port 8080 is accessible: `netstat -tulpn | grep 8080`
3. Check Docker network: `docker network inspect libhub_libhub-network`
4. Review all container logs: `docker compose logs`

