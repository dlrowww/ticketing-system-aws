import type { LayoutServerLoad } from './$types';
import { fetchLookups } from '$lib/services/Lookups';

export const load: LayoutServerLoad = async ({ fetch, locals }) => {
	let lookups = await fetchLookups(fetch);
	
	// Fetch categories server-side (SSR) and add to lookups
	let categories = [];
	try {
		const response = await fetch('/api/categories');
		if (response.ok) {
			categories = await response.json();
			// Add categories to lookups payload so lookups.category() works
			lookups = { ...lookups, category: categories };
		} else {
			console.error(`Failed to load categories: ${response.status}`);
		}
	} catch (error) {
		console.error('Failed to load categories in +layout.server.ts:', error);
	}
	
	return {
		user: locals.user,
		locale: locals.locale,
		lookups,
		categories // Pass categories to client (for categories store)
	};
}