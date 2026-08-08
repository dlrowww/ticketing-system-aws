import type { LayoutServerLoad } from './$types';
import { redirect } from '@sveltejs/kit';

export const load: LayoutServerLoad = async ({ locals, url }) => {
	if (!locals.user) {
		throw redirect(303, '/login');
	}
	// } else if (url.pathname !== '/app/unauthorized') { // TODO - add later:  && locals.user.role !== 'admin'
	//     throw redirect(303, '/app/unauthorized');
	// }

	return {
		user: locals.user,
		locale: locals.locale
	};
};
