<script lang="ts">
	import { onDestroy, onMount } from 'svelte';

	import type { CategoryDto, CategoryQuery } from '$lib/types/categories';
	import type { Sorting } from '$lib/types/table';
	import type { RowActionEvent } from '$lib/types/table';

	import DataTable from '$lib/components/tables/base/DataTable.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	import { getMessage } from '$lib/i18n';
	import { categoriesTableConfig } from '$lib/components/tables/configs/categoriesTableConfig';
	import { fetchCategories, updateCategory, deleteCategory } from '$lib/services/Categories';
	import { toastStore } from '$lib/stores/toast';
	import { modalStore } from '$lib/stores/modal';
	import CategoryFormModal from '$lib/components/modals/CategoryFormModal.svelte';
	import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';

	let {
		initial,
		initialQuery
	}: { initial?: CategoryDto[]; initialQuery?: CategoryQuery } = $props();

	let loading = $state(false);
	let errorMsg = $state<string | null>(null);

	const query = $state<CategoryQuery>({
		includeInactive: initialQuery?.includeInactive ?? true,
		sortBy: initialQuery?.sortBy ?? 'categoryId',
		sortDir: initialQuery?.sortDir ?? 'asc'
	});

	let categories = $state<CategoryDto[]>(initial ?? []);

	const sortingInfo = $derived<Sorting>({
		sortBy: query.sortBy ?? 'categoryId',
		sortDir: query.sortDir ?? 'asc'
	});

	async function load() {
		loading = true;
		errorMsg = null;
		try {
			const res = await fetchCategories(query, fetch);
			categories = res;
		} catch (e: any) {
			errorMsg = e?.message ?? getMessage('categories_load_error');
		} finally {
			loading = false;
		}
	}

	function handleRefresh() {
		load();
	}

	onMount(() => {
		window.addEventListener('categories:refresh', handleRefresh as EventListener);
	});

	onDestroy(() => {
		window.removeEventListener('categories:refresh', handleRefresh as EventListener);
	});

	let skipFirst = $state(!!initial);
	$effect(() => {
		void query.includeInactive;
		void query.sortBy;
		void query.sortDir;

		if (skipFirst) {
			skipFirst = false;
			return;
		}
		load();
	});

	function handleSortChange(sortBy: string, sortDir: 'asc' | 'desc') {
		query.sortBy = sortBy as any;
		query.sortDir = sortDir;
	}

	async function handleRowAction(event: RowActionEvent) {
		const id = Number(event.id);

		if (event.action === 'edit') {
			modalStore.open({
				component: CategoryFormModal,
				props: { categoryId: id },
				size: 'lg'
			});
			return;
		}

		if (event.action === 'toggle_active') {
			const category = categories.find((x) => x.categoryId === id);
			if (!category) return;

			try {
				await updateCategory(id, { isActive: !category.isActive }, fetch);
				toastStore.success(getMessage('category_update_success'));
				await load();
			} catch (e: any) {
				const code = (e as any)?.code as string | undefined;
				toastStore.error(
					code ? getMessage(`error_code_${code}`) : getMessage('category_update_failed')
				);
			}
			return;
		}

		if (event.action === 'delete') {
			const category = categories.find((x) => x.categoryId === id);
			if (!category) return;

			modalStore.open({
				component: ConfirmModal,
				props: {
					title: getMessage('category_delete_confirm_title'),
					message: getMessage('category_delete_confirm'),
					confirmText: getMessage('delete'),
					cancelText: getMessage('cancel'),
					variant: 'danger',
					onConfirm: async () => {
						try {
							await deleteCategory(id, fetch);
							toastStore.success(getMessage('category_delete_success'));
							await load();
						} catch (e: any) {
							const code = (e as any)?.code as string | undefined;
							toastStore.error(
								code ? getMessage(`error_code_${code}`) : getMessage('category_delete_failed')
							);
						}
					}
				},
				size: 'sm'
			});
			return;
		}
	}
</script>

<div class="d-flex flex-wrap gap-2 align-items-end mb-3">
	<div class="form-check">
		<input
			id="include-inactive"
			class="form-check-input"
			type="checkbox"
			bind:checked={query.includeInactive}
		/>
		<label class="form-check-label" for="include-inactive">
			{getMessage('category_show_inactive')}
		</label>
	</div>

	<div class="ms-auto d-flex gap-2">
		<Button
			type="button"
			variant="primary"
			size="sm"
			onclick={() =>
				modalStore.open({
					component: CategoryFormModal,
					size: 'lg'
				})}
		>
			<i class="bi bi-plus-circle me-1"></i>
			{getMessage('category_create')}
		</Button>
	</div>
</div>

<DataTable
	config={categoriesTableConfig}
	data={categories}
	{loading}
	error={errorMsg}
	sorting={sortingInfo}
	onRowAction={handleRowAction}
	onSortChange={handleSortChange}
/>
