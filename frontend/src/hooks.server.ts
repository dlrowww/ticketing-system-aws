import type { User } from '$lib/types/user';
import type { Handle, HandleFetch } from '@sveltejs/kit';

import { jwtVerify } from 'jose';
import { JWT_SECRET, BACKEND_URL } from '$env/static/private';
//import { setLocale } from '$lib/i18n';
import { setCurrentLocale } from '$lib/server/AppSetupServer';

//const JWT_SECRET = 'YOUR_SUPER_SECRET_KEY_123456789012345';

// Rewrite /api requests to backend URL when called from server-side
export const handleFetch: HandleFetch = async ({ request, fetch, event }) => {
    const url = new URL(request.url);
    
    // If the request is for /api, rewrite it to the backend container URL
    if (url.pathname.startsWith('/api')) {
        const backendUrl = BACKEND_URL || 'http://api:8080';
        // Rewrite to backend URL
        url.href = `${backendUrl}${url.pathname}${url.search}`;
        
        // Forward auth cookie from the current request context
        const headers = new Headers(request.headers);
        const authToken = event.cookies.get('auth_token');
        if (authToken) {
            headers.set('Cookie', `auth_token=${authToken}`);
        }
        
        request = new Request(url, {
            ...request,
            headers
        });
    }
    
    return fetch(request);
};

export const handle: Handle = async ({ event, resolve }) => {
    // Proxy /api requests to backend
    if (event.url.pathname.startsWith('/api')) {
        const backendUrl = BACKEND_URL || 'http://api:8080';
        const backendRequest = new URL(event.url.pathname + event.url.search, backendUrl);
        
        // Forward cookies from client request
        const cookieHeader = event.request.headers.get('cookie');
        const headers = new Headers(event.request.headers);
        if (cookieHeader) {
            headers.set('cookie', cookieHeader);
        }
        
        const response = await fetch(backendRequest.toString(), {
            method: event.request.method,
            headers: headers,
            body: event.request.method !== 'GET' && event.request.method !== 'HEAD' 
                ? await event.request.text() 
                : undefined
        });
        
        return response;
    }
    
    setCurrentLocale(event);
    
    const token = event.cookies.get('auth_token');
    if (token) {
        try {
            if (!JWT_SECRET || JWT_SECRET.length < 16) {
                console.error('JWT_SECRET is missing/too short. len=', JWT_SECRET?.length ?? 0);
                throw new Error('JWT secret not configured');
            }
            const { payload } = await jwtVerify(token, new TextEncoder().encode(JWT_SECRET), { algorithms: ['HS256'] });
            
            // Map JWT payload to User object
            event.locals.user = {
                id: payload.id as string,
                name: payload.name as string,
                email: payload.email as string,
                roleId: payload.roleId as string,
                categoryId: payload.categoryId as string | undefined
            };
        } catch (err) {
            console.error('JWT verify failed:', err);
            // Clear invalid token cookie
            event.cookies.delete('auth_token', { path: '/' });
            event.locals.user = undefined;
        }
    } else {
        event.locals.user = undefined;
    }
    return resolve(event);
}