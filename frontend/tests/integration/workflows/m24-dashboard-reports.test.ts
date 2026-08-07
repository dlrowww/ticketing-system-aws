// @vitest-environment node
import { describe, it, expect, vi } from 'vitest';
import { UserRole } from '$lib/types/enums';

import { load as loadDashboard } from '../../../src/routes/app/dashboard/+page.server';

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
			json: async () => []
		} as any;
	});
}

function fetchUrls(fetchSpy: ReturnType<typeof createFetchSpy>): string[] {
	return (fetchSpy as any).mock.calls.map((c: any[]) => String(c[0]));
}

function expectQueryParam(url: string, key: string, value: string) {
	const parsed = new URL(url, 'http://localhost');
	expect(parsed.searchParams.get(key)).toBe(value);
}

describe('Phase 2.4 Dashboard & Reports - server load', () => {
	it('redirects when user is not Admin/TeamLeader', async () => {
		const fetchSpy = createFetchSpy();
		await expect(
			loadDashboard({
				fetch: fetchSpy as any,
				url: makeUrl('/app/dashboard'),
				parent: makeParent({ id: '10', roleId: String(UserRole.Employee) })
			} as any)
		).rejects.toMatchObject({ status: 303, location: '/app/my-tickets' });
	});

	it('forwards date filters and clamps days', async () => {
		const fetchSpy = createFetchSpy();
		await loadDashboard({
			fetch: fetchSpy as any,
			url: makeUrl('/app/dashboard', 'from=2025-01-01&to=2025-01-31&days=999'),
			parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
		} as any);

		const urls = fetchUrls(fetchSpy);
		expect(urls).toHaveLength(4);

		const dashboard = urls.find((u) => u.includes('/api/reports/dashboard'))!;
		const byCategory = urls.find((u) => u.includes('/api/reports/tickets-by-category'))!;
		const byStatus = urls.find((u) => u.includes('/api/reports/tickets-by-status'))!;
		const trend = urls.find((u) => u.includes('/api/reports/ticket-trend'))!;

		expectQueryParam(dashboard, 'from', '2025-01-01');
		expectQueryParam(dashboard, 'to', '2025-01-31');
		expectQueryParam(byCategory, 'from', '2025-01-01');
		expectQueryParam(byCategory, 'to', '2025-01-31');
		expectQueryParam(byStatus, 'from', '2025-01-01');
		expectQueryParam(byStatus, 'to', '2025-01-31');

		expectQueryParam(trend, 'days', '365');
	});
});
