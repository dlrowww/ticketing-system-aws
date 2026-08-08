// src/lib/components/tables/configs/ticketsTableConfig.ts
import type { DataTableConfig, TableColumn } from '$lib/types/table';
import type { TicketListItem } from '$lib/types/tickets';
import TicketStatusBadge from '$lib/components/TicketStatusBadge.svelte';
import PriorityBadge from '$lib/components/tickets/PriorityBadge.svelte';
import { getMessage } from '$lib/i18n';
import { getCategoryName } from '$lib/stores/categories';
import { formatDateTimeForTable } from '$lib/utils/dateTime';

/** Format category with i18n - uses database-backed categories */
function formatCategory(categoryId: number): string {
	return getCategoryName(categoryId); // Locale-aware, DB-driven
}

/** Tickets table column definitions */
export const ticketsTableColumns: TableColumn<TicketListItem>[] = [
	{
		key: 'ticketId',
		label: 'ticketId',
		width: 'auto',
		sortable: true
	},
	{
		key: 'title',
		label: 'title',
		width: '40%',
		sortable: true,
		cellClass: 'text-truncate',
		formatter: (value: string) => value
	},
	{
		key: 'categoryId',
		label: 'category',
		width: 'auto',
		sortable: true,
		formatter: formatCategory
	},
	{
		key: 'priority',
		label: 'priority',
		width: 'auto',
		sortable: true,
		formatter: () => PriorityBadge
	},
	{
		key: 'status',
		label: 'status',
		width: 'auto',
		sortable: true,
		// Return component reference - DataTable will render it
		formatter: () => TicketStatusBadge
	},
	{
		key: 'createdAt',
		label: 'createdAt',
		width: 'auto',
		sortable: true,
		formatter: formatDateTimeForTable
	},
	{
		key: 'assignedToName',
		label: 'assigned_to',
		width: 'auto',
		sortable: true,
		formatter: (value: string | null | undefined) => value ?? '-'
	}
];

/** Default tickets table configuration */
export const ticketsTableConfig: DataTableConfig<TicketListItem> = {
	keyField: 'ticketId',
	columns: ticketsTableColumns,
	actions: [
		{
			id: 'view',
			label: 'action_view',
			icon: 'bi-eye'
		}
	],
	enableSelection: false,
	enableSorting: true
};
