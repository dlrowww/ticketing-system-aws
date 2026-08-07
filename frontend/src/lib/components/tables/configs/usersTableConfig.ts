import type { DataTableConfig, TableColumn } from '$lib/types/table';
import type { UserListItemDto } from '$lib/types/users';

import { getMessage } from '$lib/i18n';
import { UserRoleKey } from '$lib/types/enums';
import { getCategoryName } from '$lib/stores/categories';

function formatRole(roleId: number): string {
	return getMessage(UserRoleKey[roleId as keyof typeof UserRoleKey] ?? 'role_employee');
}

function formatCategory(categoryId: number | null | undefined): string {
	if (categoryId === null || categoryId === undefined) return '-';
	return getCategoryName(categoryId); // Locale-aware, DB-driven
}

function formatActive(isActive: boolean): string {
	return getMessage(isActive ? 'user_active' : 'user_inactive');
}

export const usersTableColumns: TableColumn<UserListItemDto>[] = [
	{ key: 'userId', label: 'user_id', width: 'auto', sortable: true },
	{ key: 'name', label: 'name', width: '25%', sortable: true, formatter: (v: string) => v },
	{ key: 'email', label: 'email', width: '30%', sortable: true, formatter: (v: string) => v },
	{ key: 'role', label: 'role', width: 'auto', sortable: true, formatter: formatRole },
	{
		key: 'categoryId',
		label: 'category',
		width: 'auto',
		sortable: true,
		formatter: formatCategory
	},
	{ key: 'isActive', label: 'user_status', width: 'auto', sortable: true, formatter: formatActive }
];

export const usersTableConfig: DataTableConfig<UserListItemDto> = {
	keyField: 'userId',
	columns: usersTableColumns,
	actions: [
		{ id: 'edit', label: 'action_edit', icon: 'bi-pencil-square' },
		{ id: 'toggle_active', label: 'action_toggle_active', icon: 'bi-person-check' }
	],
	enableSelection: false,
	enableSorting: true
};
