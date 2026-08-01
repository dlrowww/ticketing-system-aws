import { describe, it, expect, vi } from 'vitest';
import { render, waitFor } from '@testing-library/svelte';
import TicketsTable from '$lib/components/tables/TicketsTable.svelte';

const fetchTicketsMock = vi.fn();
vi.mock('$lib/services/Tickets', async () => {
	const actual = await vi.importActual<any>('$lib/services/Tickets');
	return {
		...actual,
		fetchTickets: (...args: any[]) => fetchTicketsMock(...args)
	};
});

vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

describe('TicketsTable refresh', () => {
	it('reloads tickets when tickets:refresh is dispatched', async () => {
		fetchTicketsMock.mockResolvedValue({
			items: [],
			page: 1,
			pageSize: 10,
			totalCount: 0
		});

		// Provide initial data so initial mount doesn't need to fetch.
		render(TicketsTable as any, {
			props: {
				initial: {
					items: [],
					page: 1,
					pageSize: 10,
					totalCount: 0
				}
			}
		});

		// TicketsTable currently performs an initial load on mount.
		await waitFor(() => expect(fetchTicketsMock).toHaveBeenCalledTimes(1));
		fetchTicketsMock.mockClear();

		window.dispatchEvent(new CustomEvent('tickets:refresh'));

		// Wait for the async reload to happen.
		await waitFor(() => expect(fetchTicketsMock).toHaveBeenCalledTimes(1));
	});
});
