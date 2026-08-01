import type { RequestEvent } from '@sveltejs/kit';

import { getLocaleSessionFromCookie } from './LocaleBroker';

export function setCurrentLocale(event: RequestEvent) {

    const {request, locals, cookies} = event;
    const cookieLocale = getLocaleSessionFromCookie(cookies);
    const headerLocale = request.headers.get('accept-language')?.split(',')[0];
    let currentLocale = cookieLocale || headerLocale || 'en-US';
    
    locals.locale = currentLocale;
}