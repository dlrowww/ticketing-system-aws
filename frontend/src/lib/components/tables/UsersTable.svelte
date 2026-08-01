<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import { get } from 'svelte/store';

	import type { UserListItemDto, UserQuery, PagedResult } from '$lib/types/users';
	import type { PaginationInfo, RowActionEvent, Sorting } from '$lib/types/table';

	import DataTable from '$lib/components/tables/base/DataTable.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	import { getMessage } from '$lib/i18n';
	import { usersTableConfig } from '$lib/components/tables/configs/usersTableConfig';
	import { fetchUsers, updateUser } from '$lib/services/Users';
	import { lookups, toOptions } from '$lib/lookups/Lookups';
	import { getCategoryName, categoryMap } from '$lib/stores/categories';
	import { toastStore } from '$lib/stores/toast';
	import { modalStore } from '$lib/stores/modal';
	import UserFormModal from '$lib/components/modals/UserFormModal.svelte';
	import { UserRole } from '$lib/types/enums';

	let { initial, initialQuery }: { initial?: PagedResult<UserListItemDto>; initialQuery?: UserQuery } = $props();

	let loading = $state(false);
	let errorMsg = $state<string | null>(null);

	const query = $state<UserQuery>({
		page: initialQuery?.page ?? 1,
		pageSize: initialQuery?.pageSize ?? 10,
		sortBy: initialQuery?.sortBy ?? 'userId',
		sortDir: initialQuery?.sortDir ?? 'asc',
		search: initialQuery?.search ?? '',
		role: (initialQuery?.role as any) ?? 'All',
		category: (initialQuery?.category as any) ?? 'All',
		isActive: (initialQuery?.isActive as any) ?? 'All'
	});

	let page = $state<PagedResult<UserListItemDto>>(
		initial ?? {
			items: [],
			total: 0,
			page: query.page ?? 1,
			size: query.pageSize ?? 10,
			totalPages: 1,
			hasNext: false,
			hasPrevious: false,
			sort: null
		}
	);

	const roleOptions = $derived([
		{ value: 'All', labelKey: 'all' },
		...toOptions(lookups.userRole())
	]);

	const categoryOptions = $derived([
		{ value: 'All' as const, labelKey: 'all' },
		...Array.from(get(categoryMap).values()).map(cat => ({
			value: cat.categoryId as number | 'All',
			labelKey: getCategoryName(cat.categoryId)
		}))
	]);

	const statusOptions = $derived([
		{ value: 'All', labelKey: 'all' },
		{ value: 'true', labelKey: 'active' },
		{ value: 'false', labelKey: 'inactive' }
	]);

	const paginationInfo = $derived<PaginationInfo>({
		currentPage: page.page,
		totalPages: page.totalPages,
		pageSize: page.size,
		totalItems: page.total,
		hasNext: page.hasNext,
		hasPrevious: page.hasPrevious
	});

	const sortingInfo = $derived<Sorting>({
		sortBy: query.sortBy,
		sortDir: query.sortDir
	});

	async function load() {
		loading = true;
		errorMsg = null;
		try {
			const res = await fetchUsers(query, fetch);
			page = res;
		} catch (e: any) {
			errorMsg = e?.message ?? getMessage('users_load_error');
		} finally {
			loading = false;
		}
	}

	function handleRefresh() {
		load();
	}

	onMount(() => {
		window.addEventListener('users:refresh', handleRefresh as EventListener);
	});

	onDestroy(() => {
		window.removeEventListener('users:refresh', handleRefresh as EventListener);
	});

	let skipFirst = $state(!!initial);
	$effect(() => {
		void query.page;
		void query.pageSize;
		void query.sortBy;
		void query.sortDir;
		void query.search;
		void query.role;
		void query.category;
		void query.isActive;

		if (skipFirst) {
			skipFirst = false;
			return;
		}
		load();
	});

	function handleSortChange(sortBy: string, sortDir: 'asc' | 'desc') {
		query.sortBy = sortBy as any;
		query.sortDir = sortDir;
		query.page = 1;
	}

	function handlePageChange(next: number) {
		query.page = next;
	}

	function handlePageSizeChange(size: number) {
		query.pageSize = size;
		query.page = 1;
	}

	async function handleRowAction(event: RowActionEvent) {
		const id = Number(event.id);
		if (event.action === 'edit') {
			modalStore.open({
				component: UserFormModal,
				props: { userId: id },
				size: 'lg'
			});
			return;
		}

		if (event.action === 'toggle_active') {
			const row = page.items.find((x) => x.userId === id);
			if (!row) return;

			try {
				await updateUser(id, { isActive: !row.isActive }, fetch);
				toastStore.success(getMessage('user_update_success'));
				await load();
			} catch (e: any) {
				const code = (e as any)?.code as string | undefined;
				toastStore.error(code ? getMessage(`error_code_${code}`) : getMessage('user_update_failed'));
			}
		}
	}
</script>

<div class="d-flex flex-wrap gap-2 align-items-end mb-3">
	<div class="me-2">
		<label for="users-search" class="form-label mb-1">{getMessage('search')}</label>
		<Input
			id="users-search"
			type="search"
			bind:value={query.search}
			placeholder={getMessage('search_user_placeholder')}
			ellipsis
		/>
	</div>

	<div class="me-2">
		<label for="users-role" class="form-label mb-1">{getMessage('role')}</label>
		<Select id="users-role" bind:value={query.role} options={roleOptions} widthGroup="user-filters" />
	</div>

	<div class="me-2">
		<label for="users-category" class="form-label mb-1">{getMessage('category')}</label>
		<Select id="users-category" bind:value={query.category} options={categoryOptions} widthGroup="user-filters" />
	</div>

	<div class="me-2">
		<label for="users-status" class="form-label mb-1">{getMessage('status')}</label>
		<Select id="users-status" bind:value={query.isActive} options={statusOptions} widthGroup="user-filters" />
	</div>

	<div class="ms-auto d-flex gap-2">
		<Button
			type="button"
			variant="primary"
			size="sm"
			onclick={() =>
				modalStore.open({
					component: UserFormModal,
					size: 'lg'
				})}
		>
			<i class="bi bi-person-plus me-1"></i>
			{getMessage('user_create')}
		</Button>
	</div>
</div>

<DataTable
	config={usersTableConfig}
	data={page.items}
	pagination={paginationInfo}
	sorting={sortingInfo}
	{loading}
	error={errorMsg}
	onRowAction={handleRowAction}
	onSortChange={handleSortChange}
	onPageChange={handlePageChange}
	onPageSizeChange={handlePageSizeChange}
/>
