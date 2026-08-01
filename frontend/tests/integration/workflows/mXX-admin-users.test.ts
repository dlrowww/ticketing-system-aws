// @vitest-environment node
import { describe, it, expect, vi } from 'vitest';
import { UserRole } from '$lib/types/enums';

import { load as loadAdminUsers } from '../../../src/routes/app/admin/users/+page.server';

type ParentData = {
	user: {
		id: string;
		roleId: string;
		categoryId?: string;
	};
};

function makeUrl(path: string, qs: string = ''): URL {
	return new URL(`http://localhost${path}${qs ? `?${qs}` : ''}`);
}

function makeParent(user: ParentData['user']) {
	return async () => ({ user });
}

function createFetchSpy() {
	return vi.fn(async (input: RequestInfo | URL) => {
		return {
			ok: true,
			status: 200,
			statusText: 'OK',
			json: async () => ({
				items: [],
				total: 0,
				page: 1,
				size: 10,
				totalPages: 1,
				hasNext: false,
				hasPrevious: false,
				sort: null
			})
		} as any;
	});
}

function lastFetchUrl(fetchSpy: ReturnType<typeof createFetchSpy>): string {
	const call = fetchSpy.mock.calls.at(-1);
	if (!call) throw new Error('Expected fetch to be called');
	return String(call[0]);
}

describe('Admin Users - server load', () => {
	it('redirects if user is not Admin', async () => {
		const fetchSpy = createFetchSpy();
		await expect(
			loadAdminUsers({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/users'),
				parent: makeParent({ id: '10', roleId: String(UserRole.Employee) })
			} as any)
		).rejects.toMatchObject({ status: 303 });
	});

	it('calls /api/users and omits role=All', async () => {
		const fetchSpy = createFetchSpy();
		await loadAdminUsers({
			fetch: fetchSpy as any,
			url: makeUrl('/app/admin/users', 'page=2&pageSize=25&role=All&search=test'),
			parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
		} as any);

		const url = lastFetchUrl(fetchSpy);
		expect(url).toContain('/api/users?');

		const parsed = new URL(url);
		expect(parsed.searchParams.get('page')).toBe('2');
		expect(parsed.searchParams.get('pageSize')).toBe('25');
		expect(parsed.searchParams.get('search')).toBe('test');
		expect(parsed.searchParams.get('role')).toBeNull();
	});
});
