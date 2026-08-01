import type { RequestHandler } from '@sveltejs/kit';

import { setLocaleSession } from '$lib/server/LocaleBroker';

export const POST: RequestHandler = async ({ request, cookies }) => {
    const { locale } = await request.json();
    setLocaleSession(cookies, locale);
    return new Response(JSON.stringify({ success: true }), {
        headers: { 'Content-Type': 'application/json' }
    });
};