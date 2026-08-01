import type { PageServerLoad } from './$types';

import { loadTicketsListPage } from '$lib/server/ticketsListPage';

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	const userId = Number(user?.id);

	return loadTicketsListPage({
		fetch,
		url,
		fixed: Number.isFinite(userId) ? { createdByUserId: userId } : undefined
	});
};
