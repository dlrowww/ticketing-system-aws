import type { PageServerLoad } from './$types';

import { redirect } from '@sveltejs/kit';

import { UserRole } from '$lib/types/enums';

export const load: PageServerLoad = async ({ locals }) => {
	if (!locals.user) {
		return {};
	}

	const roleId = Number(locals.user.roleId);
	if (Number.isFinite(roleId) && (roleId === UserRole.Admin || roleId === UserRole.TeamLeader)) {
		throw redirect(303, '/app/dashboard');
	}

	throw redirect(303, '/app/my-tickets');
};
