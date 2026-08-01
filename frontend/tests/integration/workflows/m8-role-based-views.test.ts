// @vitest-environment node
import { describe, it, expect, vi } from 'vitest';
import { UserRole } from '$lib/types/enums';

import { load as loadMyTickets } from '../../../src/routes/app/my-tickets/+page.server';
import { load as loadAssigned } from '../../../src/routes/app/assigned/+page.server';
import { load as loadUnassigned } from '../../../src/routes/app/unassigned/+page.server';
import { load as loadTeam } from '../../../src/routes/app/team/+page.server';

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
		// minimal Response shape used by the loaders
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
	const input = call[0] as any;
	return String(input);
}

function expectQueryParam(url: string, key: string, value: string) {
	const parsed = new URL(url);
	expect(parsed.searchParams.get(key)).toBe(value);
}

describe('M.8 Role-Based Views - server loads', () => {
	it('M.8.1 My Tickets: requests list filtered by createdByUserId', async () => {
		const fetchSpy = createFetchSpy();
		await loadMyTickets({
			fetch: fetchSpy as any,
			url: makeUrl('/app/my-tickets'),
			parent: makeParent({ id: '10', roleId: String(UserRole.Employee) })
		} as any);

		const url = lastFetchUrl(fetchSpy);
		expect(url).toContain('/api/tickets?');
		expectQueryParam(url, 'createdByUserId', '10');
	});

	it('M.8.2 My Workload: redirects if user is not Support/TeamLeader', async () => {
		const fetchSpy = createFetchSpy();
		await expect(
			loadAssigned({
				fetch: fetchSpy as any,
				url: makeUrl('/app/assigned'),
				parent: makeParent({ id: '10', roleId: String(UserRole.Employee) })
			} as any)
		).rejects.toMatchObject({ status: 303 });
	});

	it('M.8.2 My Workload: requests list filtered by assignedToUserId for Support', async () => {
		const fetchSpy = createFetchSpy();
		await loadAssigned({
			fetch: fetchSpy as any,
			url: makeUrl('/app/assigned', 'page=2&pageSize=25'),
			parent: makeParent({ id: '42', roleId: String(UserRole.Support) })
		} as any);

		const url = lastFetchUrl(fetchSpy);
		expectQueryParam(url, 'assignedToUserId', '42');
		// preserves existing paging params
		expectQueryParam(url, 'page', '2');
		expectQueryParam(url, 'pageSize', '25');
	});

	it('M.8.3 Unassigned Pool: redirects if user is not Support', async () => {
		const fetchSpy = createFetchSpy();
		await expect(
			loadUnassigned({
				fetch: fetchSpy as any,
				url: makeUrl('/app/unassigned'),
				parent: makeParent({ id: '10', roleId: String(UserRole.TeamLeader), categoryId: '1' })
			} as any)
		).rejects.toMatchObject({ status: 303 });
	});

	it('M.8.3 Unassigned Pool: shows category missing state when categoryId is absent', async () => {
		const fetchSpy = createFetchSpy();
		const res = (await loadUnassigned({
			fetch: fetchSpy as any,
			url: makeUrl('/app/unassigned'),
			parent: makeParent({ id: '10', roleId: String(UserRole.Support) })
		} as any)) as any;

		expect(res.categoryMissing).toBe(true);
		expect(fetchSpy).not.toHaveBeenCalled();
	});

	it('M.8.3 Unassigned Pool: requests list filtered by assignedToUserId=null and category', async () => {
		const fetchSpy = createFetchSpy();
		await loadUnassigned({
			fetch: fetchSpy as any,
			url: makeUrl('/app/unassigned', 'search=test'),
			parent: makeParent({ id: '10', roleId: String(UserRole.Support), categoryId: '2' })
		} as any);

		const url = lastFetchUrl(fetchSpy);
		// empty value means "unassigned" (IS NULL)
		expectQueryParam(url, 'assignedToUserId', '');
		expectQueryParam(url, 'category', '2');
		expectQueryParam(url, 'search', 'test');
	});

	it('M.8.4 Team Tickets: redirects if user is not TeamLeader', async () => {
		const fetchSpy = createFetchSpy();
		await expect(
			loadTeam({
				fetch: fetchSpy as any,
				url: makeUrl('/app/team'),
				parent: makeParent({ id: '10', roleId: String(UserRole.Support), categoryId: '1' })
			} as any)
		).rejects.toMatchObject({ status: 303 });
	});

	it('M.8.4 Team Tickets: shows category missing state when categoryId is absent', async () => {
		const fetchSpy = createFetchSpy();
		const res = (await loadTeam({
			fetch: fetchSpy as any,
			url: makeUrl('/app/team'),
			parent: makeParent({ id: '10', roleId: String(UserRole.TeamLeader) })
		} as any)) as any;

		expect(res.categoryMissing).toBe(true);
		expect(fetchSpy).not.toHaveBeenCalled();
	});

	it('M.8.4 Team Tickets: requests list filtered by category', async () => {
		const fetchSpy = createFetchSpy();
		await loadTeam({
			fetch: fetchSpy as any,
			url: makeUrl('/app/team', 'sortBy=createdAt&sortDir=desc'),
			parent: makeParent({ id: '10', roleId: String(UserRole.TeamLeader), categoryId: '3' })
		} as any);

		const url = lastFetchUrl(fetchSpy);
		expectQueryParam(url, 'category', '3');
		expectQueryParam(url, 'sortBy', 'createdAt');
		expectQueryParam(url, 'sortDir', 'desc');
	});
});
