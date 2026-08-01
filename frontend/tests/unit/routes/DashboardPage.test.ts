import { render, within } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';

import DashboardPage from '../../../src/routes/app/dashboard/+page.svelte';
import { Category, TicketStatus } from '$lib/types/enums';

describe('Dashboard +page.svelte', () => {
	it('renders KPI cards and aggregations', () => {
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
					byCategory: [{ category: Category.It, count: 4 }],
					byStatus: [{ status: TicketStatus.StatusOpen, count: 2 }],
					trend: [{ date: '2025-01-01', count: 1 }]
				} as any)
			}
		});

		// When i18n isn't awaited (unit tests), getMessage falls back to keys.
		expect(getByText('dashboard_title')).toBeInTheDocument();
		expect(getByText('dashboard_kpi_total')).toBeInTheDocument();

		// Scope KPI value assertion (DatePicker renders calendar day buttons too)
		const totalLabel = getByText('dashboard_kpi_total');
		const totalCard = totalLabel.closest('.card');
		expect(totalCard).toBeTruthy();
		expect(within(totalCard as HTMLElement).getByText('10')).toBeInTheDocument();

		// Dashboard renders charts (canvas) when data is present
		const canvases = container.querySelectorAll('canvas');
		expect(canvases.length).toBeGreaterThan(0);
	});
});
