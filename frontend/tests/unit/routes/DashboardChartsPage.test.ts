import { render, within } from '@testing-library/svelte';
import { describe, it, expect, vi } from 'vitest';

vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

import DashboardPage from '../../../src/routes/app/dashboard/+page.svelte';
import { TicketStatus } from '$lib/types/enums';

describe('Dashboard +page.svelte (charts)', () => {
	it('renders KPI cards and charts', () => {
		const { getByText, container } = render(DashboardPage, {
			props: {
				data: ({
					filters: { from: null, to: null, days: 30 },
					stats: {
						totalTickets: 10,
						openTickets: 3,
						inProgressTickets: 2,
						resolvedTickets: 5,
						averageResolutionTimeHours: 12.5
					},
					byCategory: [{ categoryId: 1, count: 4 }],
					byStatus: [{ status: TicketStatus.Open, count: 2 }],
					trend: [{ date: '2025-01-01', count: 1 }]
				} as any)
			}
		});

		// getMessage falls back to keys in unit tests
		expect(getByText('dashboard_title')).toBeInTheDocument();
		expect(getByText('dashboard_kpi_total')).toBeInTheDocument();

		// Scope the KPI value assertion (DatePicker renders calendar day buttons too)
		const totalLabel = getByText('dashboard_kpi_total');
		const totalCard = totalLabel.closest('.card');
		expect(totalCard).toBeTruthy();
		expect(within(totalCard as HTMLElement).getByText('10')).toBeInTheDocument();

		// Chart canvases exist
		const canvases = container.querySelectorAll('canvas');
		expect(canvases.length).toBeGreaterThan(0);
	});
});
