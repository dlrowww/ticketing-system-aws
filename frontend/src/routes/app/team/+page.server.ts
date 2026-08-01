import type { PageServerLoad } from './$types';

import { UserRole } from '$lib/types/enums';

import { redirect } from '@sveltejs/kit';

import { buildTicketsSearchParams, emptyTicketsPage, loadTicketsListPage } from '$lib/server/ticketsListPage';

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	const roleId = Number(user?.roleId);
	if (!Number.isFinite(roleId) || roleId !== UserRole.TeamLeader) {
		throw redirect(303, '/app/my-tickets');
	}

	const categoryId = Number(user?.categoryId);
	if (!Number.isFinite(categoryId)) {
		const params = buildTicketsSearchParams(url);
		return {
			categoryMissing: true,
			initial: emptyTicketsPage(params),
			query: {} as any
		};
	}

	return {
		categoryMissing: false,
		...(await loadTicketsListPage({
			fetch,
			url,
			fixed: { category: categoryId }
		}))
	};
};
