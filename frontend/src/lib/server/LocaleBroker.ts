import type { Cookies } from '@sveltejs/kit';

const cookieName = 'locale';
const cookieConfig = {
  secret: 'keyboard cat secret',
  path: '/',
  httpOnly: true,
  secure: process.env.NODE_ENV === 'production', // HTTPS-only in production
  maxAge: 400 * 24 * 60 * 60 * 1000 //400 days in milliseconds - the max allowed by Chrome
};

export function clearLocaleSession(cookies: Cookies, locals: App.Locals) {
	cookies.delete(cookieName, cookieConfig);
	locals.locale = undefined;
}

export function getLocaleSessionFromCookie(cookies: Cookies) {
	const jwt = cookies.get(cookieName);
	const localeData = jwt ? JSON.parse(atob(jwt)) : null;
	return localeData;
}

export function setLocaleSession(cookies: Cookies, locale: string) {
	if(locale) {
		const value = btoa(JSON.stringify(locale));
		cookies.set(cookieName, value, cookieConfig);
	}
}