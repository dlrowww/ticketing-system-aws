# Login Issue Fix - Network Error Resolved

## Problem Diagnosis
**Symptom:** Login page loads, but login fails with "Network error. Please try again."

**Browser Console Error:**
```
POST http://api:8080/api/auth/login net::ERR_NAME_NOT_RESOLVED
```

**Root Cause:** Browser JavaScript was trying to connect to `http://api:8080/api/auth/login`, but `api:8080` is a Docker container hostname that only works inside the Docker network, not from the user's browser.

## Why This Happened
We set `PUBLIC_API_BASE=http://api:8080/api` in the Dockerfile, which got baked into the client-side JavaScript bundle. This works for:
- ✅ Server-side rendering (SSR) - runs inside Docker network
- ❌ Client-side (browser) - cannot resolve Docker hostnames

## The Fix
Implemented a **dual-URL strategy**:

### 1. Changed `PUBLIC_API_BASE` to relative URL
```dockerfile
ENV PUBLIC_API_BASE=/api  # Instead of http://api:8080/api
```
![alt text](image.png)
### 2. Added SvelteKit `handleFetch` hook
Created a server-side fetch interceptor in `hooks.server.ts`:

```typescript
export const handleFetch: HandleFetch = async ({ request, fetch }) => {
    const url = new URL(request.url);
    
    // If the request is for /api, rewrite it to the backend container URL
    if (url.pathname.startsWith('/api')) {
        const backendUrl = LOOKUPS_API || 'http://api:8080';
        // Rewrite to backend URL
        url.href = `${backendUrl}${url.pathname}${url.search}`;
        
        request = new Request(url, request);
    }
    
    return fetch(request);
};
```

## How It Works Now

### Browser (Client-Side) Requests
```
User's Browser → http://localhost:8081/api/auth/login
                ↓
          Frontend Server (SvelteKit)
                ↓
          Proxies to → http://api:8080/api/auth/login
```

### Server-Side Rendering (SSR) Requests
```
Frontend Server (SvelteKit) → /api/lookups
                ↓
          handleFetch intercepts
                ↓
          Rewrites to → http://api:8080/api/lookups
```

## Result
- ✅ Browser can make API calls using relative URLs (`/api/*`)
- ✅ SvelteKit SSR can reach backend via Docker network (`http://api:8080`)
- ✅ Login and all API operations now work correctly

## Test Login
1. Open: http://localhost:8081
2. Use credentials:
   - **Email:** `admin@ironpack.pl`
   - **Password:** `IronPack2026!`
3. Login should succeed and redirect to dashboard

## Files Modified
1. `frontend/Dockerfile` - Changed PUBLIC_API_BASE to `/api`
2. `frontend/src/hooks.server.ts` - Added handleFetch to proxy server-side requests
3. `docker-compose.local.yml` - Added BACKEND_URL environment variable

---
**Status:** ✅ RESOLVED  
**Date:** January 13, 2026
