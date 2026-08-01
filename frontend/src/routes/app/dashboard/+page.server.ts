import type { PageServerLoad } from './$types';

import { redirect } from '@sveltejs/kit';

import { UserRole } from '$lib/types/enums';
import { fetchDashboardStats, fetchTicketTrend, fetchTicketsByCategory, fetchTicketsByStatus, fetchTicketsByPriority } from '$lib/services/Reports';

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	const roleId = Number(user?.roleId);
	if (!Number.isFinite(roleId) || (roleId !== UserRole.Admin && roleId !== UserRole.TeamLeader)) {
		throw redirect(303, '/app/my-tickets');
	}

	const from = url.searchParams.get('from');
	const to = url.searchParams.get('to');
	const days = Number(url.searchParams.get('days') ?? '30');
	const safeDays = Number.isFinite(days) ? Math.min(Math.max(days, 1), 365) : 30;

	const isAdmin = roleId === UserRole.Admin;

	return {
		filters: { from, to, days: safeDays },
		stats: await fetchDashboardStats({ from, to }, fetch),
		byCategory: isAdmin ? await fetchTicketsByCategory({ from, to }, fetch) : [],
		byPriority: !isAdmin ? await fetchTicketsByPriority({ from, to }, fetch) : [],
		byStatus: await fetchTicketsByStatus({ from, to }, fetch),
		trend: await fetchTicketTrend({ from, to, days: safeDays }, fetch)
	};
};
