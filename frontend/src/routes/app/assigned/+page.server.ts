import type { PageServerLoad } from './$types';
import { UserRole } from '$lib/types/enums';

import { redirect } from '@sveltejs/kit';

import { loadTicketsListPage } from '$lib/server/ticketsListPage';

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	const roleIdNum = Number(user?.roleId);
	if (!Number.isFinite(roleIdNum)) {
		throw redirect(303, '/app/my-tickets');
	}

	const roleId = roleIdNum as UserRole;
	if (roleId !== UserRole.Support && roleId !== UserRole.TeamLeader) {
		throw redirect(303, '/app/my-tickets');
	}

	const userId = Number(user?.id);
	return loadTicketsListPage({
		fetch,
		url,
		fixed: Number.isFinite(userId) ? { assignedToUserId: userId } : undefined
	});
};
