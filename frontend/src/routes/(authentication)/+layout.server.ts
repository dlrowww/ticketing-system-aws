import type { LayoutServerLoad } from './$types';

export const load: LayoutServerLoad = ({ locals }) => {
	console.error('Auth server layout > locale = ' + locals.locale);
	return {
		locale: locals.locale
	};
};
