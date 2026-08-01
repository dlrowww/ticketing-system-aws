<script lang="ts">
    import { onDestroy, onMount } from 'svelte';
    import { get } from 'svelte/store';
    import type { TicketListItem, TicketQuery, PagedResult } from '$lib/types/tickets';
    import type { RowActionEvent, BulkActionEvent, PaginationInfo, Sorting } from '$lib/types/table';
    import DataTable from '$lib/components/tables/base/DataTable.svelte';
    import TableToolbar from '$lib/components/tables/base/TableToolbar.svelte';
    import { ticketsTableConfig } from '$lib/components/tables/configs/ticketsTableConfig';
    import { fetchTickets } from '$lib/services/Tickets';
    import { lookups, toOptions } from '$lib/lookups/Lookups';
    import { getCategoryName, categoryMap } from '$lib/stores/categories';
    import { getMessage } from '$lib/i18n';
    import Button from '$lib/components/ui/Button.svelte';
    import TicketFormModal from '$lib/components/modals/TicketFormModal.svelte';
    import { modalStore } from '$lib/stores/modal';

    let {
        initial,
        initialQuery,
        onRow,
        onBulk,
        onExport,
        enableSearch = true,
        enableFilters = true,
        enableExportCsv = true
    }: {
        initial?: PagedResult<TicketListItem>;
        initialQuery?: TicketQuery;
        onRow?: (payload: { action: string; id: number }) => void;
        onBulk?: (payload: { action: string; ids: number[] }) => void;
        onExport?: () => void;
        enableSearch?: boolean;
        enableFilters?: boolean;
        enableExportCsv?: boolean;
    } = $props();

    // State
    let loading = $state(false);
    let errorMsg = $state<string | null>(null);

    // Query state
    const query = $state<TicketQuery>({
        page: initialQuery?.page ?? 1,
        pageSize: initialQuery?.pageSize ?? 10,
        sortBy: initialQuery?.sortBy ?? 'createdAt',
        sortDir: initialQuery?.sortDir ?? 'desc',
        search: initialQuery?.search ?? '',
        status: initialQuery?.status ?? 'All',
        category: initialQuery?.category ?? 'All',
        priority: initialQuery?.priority ?? 'All',
        dateFrom: initialQuery?.dateFrom,
        dateTo: initialQuery?.dateTo,
        createdByUserId: initialQuery?.createdByUserId,
        assignedToUserId: initialQuery?.assignedToUserId
    });

    // Page data
    const qSort = `${query.sortBy ?? 'createdAt'}:${query.sortDir ?? 'desc'}`;
    let page = $state<PagedResult<TicketListItem>>(
        initial ?? {
            items: [],
            total: 0,
            page: query.page ?? 1,
            size: query.pageSize ?? 10,
            totalPages: 1,
            hasNext: false,
            hasPrevious: false,
            sort: qSort
        }
    );

    // Lookups for filters
    const statusOptions = $derived(toOptions(lookups.ticketStatus()));
	const categoryOptions = $derived(
		Array.from(get(categoryMap).values()).map(cat => ({
			value: cat.categoryId as number | 'All',
			labelKey: getCategoryName(cat.categoryId)
		}))
	);
	const priorityOptions = $derived(toOptions(lookups.priority()));

    // Pagination info for DataTable
    const paginationInfo = $derived<PaginationInfo>({
        currentPage: page.page,
        totalPages: page.totalPages,
        pageSize: page.size,
        totalItems: page.total,
        hasNext: page.hasNext,
        hasPrevious: page.hasPrevious
    });

    // Sorting info for DataTable
    const sortingInfo = $derived<Sorting>({
        sortBy: query.sortBy,
        sortDir: query.sortDir
    });

    // Fetch tickets
    async function load() {
        loading = true;
        errorMsg = null;
        try {
            const res = await fetchTickets(query, fetch);
            page = res;
        } catch (e: any) {
            errorMsg = e?.message ?? getMessage('tickets_load_error');
        } finally {
            loading = false;
        }
    }

    // Allow global refresh trigger (used by TicketFormModal).
    function handleRefresh() {
        load();
    }

    onMount(() => {
        window.addEventListener('tickets:refresh', handleRefresh as EventListener);
    });

    onDestroy(() => {
        window.removeEventListener('tickets:refresh', handleRefresh as EventListener);
    });

    // Auto-load on query changes
    let skipFirst = $state(!!initial);
    $effect(() => {
        const fixedCategory = initialQuery?.category;
        const categoryIsFixed = fixedCategory !== undefined && fixedCategory !== 'All';
        if (categoryIsFixed && query.category !== fixedCategory) {
            query.category = fixedCategory as any;
        }

        void query.page;
        void query.pageSize;
        void query.sortBy;
        void query.sortDir;
        void query.search;
        void query.status;
        void query.category;
        void query.priority;
        void query.dateFrom;
        void query.dateTo;
        void query.createdByUserId;
        void query.assignedToUserId;
        
        if (skipFirst) {
            skipFirst = false;
            return;
        }
        load();
    });

    // Event handlers
    function handleRowAction(event: RowActionEvent) {
        onRow?.({ action: event.action, id: Number(event.id) });
    }

    function handleBulkAction(event: BulkActionEvent) {
        onBulk?.({ action: event.action, ids: event.ids.map(Number) });
    }

    function handleSortChange(sortBy: string, sortDir: 'asc' | 'desc') {
        query.sortBy = sortBy as any;
        query.sortDir = sortDir;
        query.page = 1;
    }

    function handlePageChange(page: number) {
        query.page = page;
    }

    function handlePageSizeChange(size: number) {
        query.pageSize = size;
        query.page = 1;
    }

    function openCreateTicket() {
        modalStore.open({
            component: TicketFormModal,
            size: 'lg'
        });
    }

    function handleExport() {
        onExport?.();
    }
</script>

<div class="d-flex flex-column h-100" style="min-height: 0;">
    <div class="tickets-toolbar-row d-flex gap-2 align-items-end mb-3">
        <div class="tickets-toolbar flex-grow-1">
            <TableToolbar
                enableSearch={enableSearch}
                enableFilters={enableFilters}
                enableExport={false}
                bind:search={query.search}
                bind:status={query.status}
                bind:category={query.category}
                bind:priority={query.priority}
                bind:dateFrom={query.dateFrom}
                bind:dateTo={query.dateTo}
                statusOptions={[{ value: 'All', labelKey: 'all' }, ...statusOptions]}
                categoryOptions={[{ value: 'All', labelKey: 'all' }, ...categoryOptions]}
                priorityOptions={[{ value: 'All', labelKey: 'all' }, ...priorityOptions]}
            />
        </div>

        <div class="tickets-actions d-flex gap-2 ms-auto align-items-end">
            {#if enableExportCsv && onExport}
                <button
                    type="button"
                    class="icon-action-btn"
                    title={getMessage('export_csv_tooltip')}
                    aria-label={getMessage('export_csv_tooltip')}
                    onclick={handleExport}
                >
                    <i class="bi bi-download"></i>
                </button>
            {/if}

            <Button type="button" variant="primary" size="sm" onclick={openCreateTicket}>
                <i class="bi bi-plus-circle me-1"></i>
                {getMessage('ticket_create_submit')}
            </Button>
        </div>
    </div>

    <div class="flex-grow-1 d-flex flex-column" style="min-height: 0;">
        <DataTable
            config={ticketsTableConfig}
            data={page.items}
            pagination={paginationInfo}
            sorting={sortingInfo}
            {loading}
            error={errorMsg}
            onRowAction={handleRowAction}
            onBulkAction={handleBulkAction}
            onSortChange={handleSortChange}
            onPageChange={handlePageChange}
            onPageSizeChange={handlePageSizeChange}
        />
    </div>
</div>

<style>
    .tickets-toolbar-row {
        gap: 0.5rem;
    }

    @media (min-width: 992px) {
        .tickets-toolbar-row {
            flex-wrap: nowrap !important;
        }
    }

    .tickets-toolbar {
        min-width: 0;
    }

    .tickets-actions {
        flex-shrink: 0;
    }

    .icon-action-btn {
        display: inline-flex;
        align-items: center;
        justify-content: center;
        width: 2.5rem;
        height: 2.5rem;
        border: none;
        border-radius: 0.25rem;
        background: transparent;
        color: var(--ironpack-font-black);
        font-size: 1.125rem;
        transition: color 0.15s ease-in-out;
        cursor: pointer;
    }

    .icon-action-btn:hover {
        color: var(--ironpack-red);
    }

    .icon-action-btn:focus-visible {
        outline: 2px solid var(--ironpack-red);
        outline-offset: 2px;
    }

    .icon-action-btn:disabled {
        color: var(--bs-secondary-color);
        cursor: not-allowed;
    }
</style>
