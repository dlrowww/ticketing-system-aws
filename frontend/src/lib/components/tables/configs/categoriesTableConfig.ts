import type { DataTableConfig, TableColumn } from '$lib/types/table';
import type { CategoryDto } from '$lib/types/categories';

import { getMessage } from '$lib/i18n';
import { formatDateTimeForTable } from '$lib/utils/dateTime';

function formatActive(isActive: boolean): string {
	return getMessage(isActive ? 'category_active' : 'category_inactive');
}

export const categoriesTableColumns: TableColumn<CategoryDto>[] = [
	{ key: 'categoryId', label: 'category_id', width: 'auto', sortable: true },
	{
		key: 'namePl',
		label: 'category_name_pl',
		width: '25%',
		sortable: true,
		formatter: (v: string) => v
	},
	{
		key: 'nameEn',
		label: 'category_name_en',
		width: '25%',
		sortable: true,
		formatter: (v: string) => v
	},
	{
		key: 'isActive',
		label: 'category_status',
		width: 'auto',
		sortable: true,
		formatter: formatActive
	},
	{
		key: 'createdAt',
		label: 'createdAt',
		width: 'auto',
		sortable: false,
		formatter: formatDateTimeForTable
	},
	{
		key: 'updatedAt',
		label: 'updatedAt',
		width: 'auto',
		sortable: false,
		formatter: formatDateTimeForTable
	}
];

export const categoriesTableConfig: DataTableConfig<CategoryDto> = {
	keyField: 'categoryId',
	columns: categoriesTableColumns,
	actions: [
		{ id: 'edit', label: 'action_edit', icon: 'bi-pencil-square' },
		{ id: 'toggle_active', label: 'action_toggle_active', icon: 'bi-toggle-on' },
		{ id: 'delete', label: 'action_delete', icon: 'bi-trash' }
	],
	enableSelection: false,
	enableSorting: true
};
