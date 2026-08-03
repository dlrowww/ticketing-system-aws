import { describe, it, expect, vi, beforeEach } from 'vitest';

import { UserRole } from '$lib/types/enums';

vi.mock('$lib/services/Reports', () => {
	return {
		fetchDashboardStats: vi.fn().mockResolvedValue({
			totalTickets: 1,
			openTickets: 1,
			inProgressTickets: 0,
			resolvedTickets: 0,
			averageResolutionTimeHours: 0
		}),
		fetchTicketsByCategory: vi.fn().mockResolvedValue([]),
		fetchTicketsByStatus: vi.fn().mockResolvedValue([]),
		fetchTicketTrend: vi.fn().mockResolvedValue([])
	};
});

describe('Dashboard (Charts) +page.server load', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it('redirects non-Admin/TeamLeader users to /app/my-tickets', async () => {
		const { load } = await import('../../../src/routes/app/dashboard/+page.server');

		await expect(
			load({
				fetch: vi.fn() as any,
				url: new URL('http://localhost/app/dashboard'),
				parent: async () => ({ user: { roleId: UserRole.Employee } })
			} as any)
		).rejects.toMatchObject({ status: 303, location: '/app/my-tickets' });
	});

	it('loads report data for Admin and clamps days to 365', async () => {
		const { load } = await import('../../../src/routes/app/dashboard/+page.server');
		const reports = await import('$lib/services/Reports');

		const result = (await load({
			fetch: vi.fn() as any,
			url: new URL('http://localhost/app/dashboard?from=2025-01-01&to=2025-01-31&days=999'),
			parent: async () => ({ user: { roleId: UserRole.Admin } })
		} as any)) as any;

		expect(result.filters).toEqual({ from: '2025-01-01', to: '2025-01-31', days: 365 });

		expect(reports.fetchDashboardStats).toHaveBeenCalledWith({ from: '2025-01-01', to: '2025-01-31' }, expect.any(Function));
		expect(reports.fetchTicketsByCategory).toHaveBeenCalledWith({ from: '2025-01-01', to: '2025-01-31' }, expect.any(Function));
		expect(reports.fetchTicketsByStatus).toHaveBeenCalledWith({ from: '2025-01-01', to: '2025-01-31' }, expect.any(Function));
		expect(reports.fetchTicketTrend).toHaveBeenCalledWith(
			{ from: '2025-01-01', to: '2025-01-31', days: 365 },
			expect.any(Function)
		);
	});
});
