import { redirect } from '@sveltejs/kit';
import type { PageServerLoad } from './$types';

import { fetchCategories } from '$lib/services/Categories';
import type { CategoryQuery } from '$lib/types/categories';
import { UserRole } from '$lib/types/enums';

export const load: PageServerLoad = async ({ locals, fetch, url }) => {
	if (!locals.user) {
		throw redirect(302, '/login');
	}

	// Admin only
	if (Number(locals.user.roleId) !== UserRole.Admin) {
		throw redirect(302, '/app/dashboard');
	}

	const query: CategoryQuery = {
		includeInactive: url.searchParams.get('includeInactive') === 'true',
		sortBy: (url.searchParams.get('sortBy') as any) ?? 'categoryId',
		sortDir: (url.searchParams.get('sortDir') as 'asc' | 'desc') ?? 'asc'
	};

	try {
		const initial = await fetchCategories(query, fetch);
		return { initial, query };
	} catch (e) {
		console.error('Failed to load categories:', e);
		return {
			initial: [],
			query,
			error: 'Failed to load categories. Please try again later.'
		};
	}
};
