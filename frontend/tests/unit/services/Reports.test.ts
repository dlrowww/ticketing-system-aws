import { describe, it, expect, vi } from 'vitest';

import {
	fetchDashboardStats,
	fetchTicketTrend,
	fetchTicketsByCategory,
	fetchTicketsByStatus
} from '$lib/services/Reports';

describe('Reports service', () => {
	it('fetchDashboardStats calls correct endpoint with date params', async () => {
		const mockFetch = vi.fn().mockResolvedValue({
			ok: true,
			json: async () => ({
				totalTickets: 10,
				openTickets: 3,
				inProgressTickets: 2,
				resolvedTickets: 5,
				averageResolutionTimeHours: 12.5
			})
		});

		await fetchDashboardStats({ from: '2025-01-01', to: '2025-01-31' }, mockFetch as any);

		expect(mockFetch).toHaveBeenCalledTimes(1);
		const [url, init] = mockFetch.mock.calls[0];
		expect(String(url)).toBe('/api/reports/dashboard?from=2025-01-01&to=2025-01-31');
		expect(init).toMatchObject({ credentials: 'include' });
	});

	it('fetchTicketsByCategory throws on non-OK response', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: false, status: 500, statusText: 'Error' });

		await expect(
			fetchTicketsByCategory({ from: null, to: null }, mockFetch as any)
		).rejects.toThrow(/Failed to load tickets by category/);
	});

	it('fetchTicketsByStatus calls correct endpoint', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] });

		await fetchTicketsByStatus({ from: '2025-02-01', to: null }, mockFetch as any);

		const [url] = mockFetch.mock.calls[0];
		expect(String(url)).toBe('/api/reports/tickets-by-status?from=2025-02-01');
	});

	it('fetchTicketTrend calls correct endpoint', async () => {
		const mockFetch = vi.fn().mockResolvedValue({ ok: true, json: async () => [] });

		await fetchTicketTrend({ days: 30 }, mockFetch as any);

		const [url] = mockFetch.mock.calls[0];
		expect(String(url)).toBe('/api/reports/ticket-trend?days=30');
	});
});
