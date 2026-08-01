// SvelteKit exposes PUBLIC_* to browser, private vars only on server.
// Prefer these typed imports over import.meta.env.
import { PUBLIC_API_BASE, PUBLIC_DEFAULT_LOCALE } from '$env/static/public';

// Export typed constants for app-wide use.
export const API_BASE = PUBLIC_API_BASE || '/api';
export const DEFAULT_LOCALE = PUBLIC_DEFAULT_LOCALE || 'en-US';