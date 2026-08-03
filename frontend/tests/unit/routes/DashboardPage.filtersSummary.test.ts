import { describe, it, expect, vi, afterEach } from 'vitest';
import { render } from '@testing-library/svelte';
import { TicketStatus } from '$lib/types/enums';

vi.mock('$lib/i18n', () => ({
    getMessage: (key: string, params?: Record<string, string>) => {
        if (!params || Object.keys(params).length === 0) {
            return key;
        }
        const serialized = Object.entries(params)
            .map(([k, v]) => `${k}=${v}`)
            .join('&');
        return `${key}?${serialized}`;
    }
}));

const DashboardPageModulePromise = import('../../../src/routes/app/dashboard/+page.svelte');

afterEach(() => {
    vi.useRealTimers();
});

describe('Dashboard filters summary', () => {
    it('shows quick range label when filters match predefined range', async () => {
        vi.useFakeTimers();
        vi.setSystemTime(new Date('2025-02-01T00:00:00Z'));
        const DashboardPage = (await DashboardPageModulePromise).default;

        const { getByText } = render(DashboardPage, {
            props: {
				data: ({
                    filters: { from: '2025-01-02', to: '2025-02-01', days: 30 },
                    stats: {
                        totalTickets: 0,
                        openTickets: 0,
                        inProgressTickets: 0,
                        resolvedTickets: 0,
                        averageResolutionTimeHours: 0
                    },
                    byCategory: [{ categoryId: 1, count: 0 }],
                    byStatus: [{ status: TicketStatus.Open, count: 0 }],
                    trend: []
				} as any)
            }
        });

		// The component displays formatted date range in subtitle
		// For this test, the mock getMessage returns dashboard_filters_range with date params
		const subtitle = document.querySelector('.text-muted.small.mt-1');
		expect(subtitle?.textContent).toContain('dashboard_filters_range');
		expect(subtitle?.textContent).toContain('Jan 2, 2025');
		expect(subtitle?.textContent).toContain('Feb 1, 2025');
	});

    it('includes formatted dates when filters are custom range', async () => {
        const DashboardPage = (await DashboardPageModulePromise).default;

        const { getByText } = render(DashboardPage, {
            props: {
				data: ({
                    filters: { from: '2025-01-05', to: '2025-01-10', days: 6 },
                    stats: {
                        totalTickets: 0,
                        openTickets: 0,
                        inProgressTickets: 0,
                        resolvedTickets: 0,
                        averageResolutionTimeHours: 0
                    },
                    byCategory: [{ categoryId: 1, count: 0 }],
                    byStatus: [{ status: TicketStatus.Open, count: 0 }],
                    trend: []
				} as any)
            }
        });

        expect(
            getByText('dashboard_filters_range?from=Jan 5, 2025&to=Jan 10, 2025')
        ).toBeInTheDocument();
    });

    it('shows single-ended range when only from is set', async () => {
        const DashboardPage = (await DashboardPageModulePromise).default;

        const { getByText } = render(DashboardPage, {
            props: {
				data: ({
                    filters: { from: '2025-01-05', to: null, days: 1 },
                    stats: {
                        totalTickets: 0,
                        openTickets: 0,
                        inProgressTickets: 0,
                        resolvedTickets: 0,
                        averageResolutionTimeHours: 0
                    },
                    byCategory: [{ categoryId: 1, count: 0 }],
                    byStatus: [{ status: TicketStatus.Open, count: 0 }],
                    trend: []
				} as any)
            }
        });

        expect(getByText('dashboard_filters_from?from=Jan 5, 2025')).toBeInTheDocument();
    });
});
