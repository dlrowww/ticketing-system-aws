import type { PageServerLoad } from './$types';

import { redirect } from '@sveltejs/kit';

import { LOOKUPS_API } from '$env/static/private';
import { UserRole } from '$lib/types/enums';
import type { PagedResult, UserListItemDto, UserQuery } from '$lib/types/users';

function toNumberOrUndefined(v: string | null): number | undefined {
	if (v === null || v.trim() === '') return undefined;
	const n = Number(v);
	return Number.isFinite(n) ? n : undefined;
}

function emptyUsersPage(params: URLSearchParams): PagedResult<UserListItemDto> {
	const page = toNumberOrUndefined(params.get('page')) ?? 1;
	const size = toNumberOrUndefined(params.get('pageSize')) ?? 10;

	return {
		items: [],
		total: 0,
		page,
		size,
		totalPages: 1,
		hasNext: false,
		hasPrevious: false,
		sort: null
	};
}

function toUsersQueryString(params: URLSearchParams): string {
	const next = new URLSearchParams();

	const page = toNumberOrUndefined(params.get('page'));
	const pageSize = toNumberOrUndefined(params.get('pageSize'));
	const role = params.get('role');
	const category = params.get('category');
	const isActive = params.get('isActive');
	const search = params.get('search');
	const sortBy = params.get('sortBy');
	const sortDir = params.get('sortDir');

	if (page) next.set('page', String(page));
	if (pageSize) next.set('pageSize', String(pageSize));
	if (search) next.set('search', search);
	if (sortBy) next.set('sortBy', sortBy);
	if (sortDir) next.set('sortDir', sortDir);
	if (role && role !== 'All') {
		const roleNumber = toNumberOrUndefined(role);
		if (roleNumber !== undefined) next.set('role', String(roleNumber));
	}
	if (category && category !== 'All') {
		const categoryNumber = toNumberOrUndefined(category);
		if (categoryNumber !== undefined) next.set('category', String(categoryNumber));
	}
	if (isActive && isActive !== 'All') {
		next.set('isActive', isActive);
	}

	return next.toString();
}

export const load: PageServerLoad = async ({ fetch, url, parent }) => {
	const { user } = await parent();
	const roleId = Number(user?.roleId);
	if (!Number.isFinite(roleId) || roleId !== UserRole.Admin) {
		throw redirect(303, '/app/unauthorized');
	}

	const backend = LOOKUPS_API || 'http://localhost:5192';
	const params = new URLSearchParams(url.searchParams);
	const qs = toUsersQueryString(params);

	const role = toNumberOrUndefined(params.get('role'));
	const category = toNumberOrUndefined(params.get('category'));
	const isActive = params.get('isActive');

	const query: UserQuery = {
		page: toNumberOrUndefined(params.get('page')),
		pageSize: toNumberOrUndefined(params.get('pageSize')),
		sortBy: (params.get('sortBy') as any) ?? undefined,
		sortDir: (params.get('sortDir') as any) ?? undefined,
		search: params.get('search') ?? undefined,
		role: (role ?? undefined) as any,
		category: (category ?? undefined) as any,
		isActive: (isActive === 'true' || isActive === 'false' ? isActive : undefined) as any
	};

	try {
		const res = await fetch(`${backend}/api/users?${qs}`, { credentials: 'include' });
		if (!res.ok) {
			return {
				initial: emptyUsersPage(params),
				query,
				error: `Users request failed: ${res.status} ${res.statusText}`
			};
		}
		const data = (await res.json()) as PagedResult<UserListItemDto>;
		return { initial: data, query };
	} catch (e: any) {
		return {
			initial: emptyUsersPage(params),
			query,
			error: e?.message ?? 'Network error'
		};
	}
};
