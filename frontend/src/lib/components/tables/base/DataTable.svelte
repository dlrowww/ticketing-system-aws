<script lang="ts" generics="T extends Record<string, any>">
    import type { 
        DataTableConfig, 
        RowActionEvent, 
        BulkActionEvent,
        Sorting,
        PaginationInfo 
    } from '$lib/types/table';
    import { getMessage } from '$lib/i18n';
    import Pagination from './Pagination.svelte';
    import SkeletonLoader from '$lib/components/ui/SkeletonLoader.svelte';

    let {
        config,
        data = [],
        pagination,
        sorting,
        loading = false,
        error,
        onRowAction,
        onBulkAction,
        onSortChange,
        onPageChange,
        onPageSizeChange
    }: {
        config: DataTableConfig<T>;
        data?: T[];
        pagination?: PaginationInfo;
        sorting?: Sorting;
        loading?: boolean;
        error?: string | null;
        onRowAction?: (event: RowActionEvent) => void;
        onBulkAction?: (event: BulkActionEvent) => void;
        onSortChange?: (sortBy: string, sortDir: 'asc' | 'desc') => void;
        onPageChange?: (page: number) => void;
        onPageSizeChange?: (size: number) => void;
    } = $props();

    // Selection state
    let selected = $state<Set<number | string>>(new Set());
    
    // Select all checkbox reference for indeterminate state
    let selectAllEl = $state<HTMLInputElement | null>(null);

    // Compute select all checkbox state
    const allSelected = $derived(data.length > 0 && selected.size === data.length);
    const someSelected = $derived(selected.size > 0 && selected.size < data.length);
    
    // Update indeterminate state
    $effect(() => {
        if (selectAllEl) {
            selectAllEl.indeterminate = someSelected;
        }
    });

    // Selection handlers
    function toggleRow(key: number | string, checked: boolean) {
        const next = new Set(selected);
        if (checked) {
            next.add(key);
        } else {
            next.delete(key);
        }
        selected = next;
    }

    function toggleAll(checked: boolean) {
        if (checked) {
            selected = new Set(data.map(row => row[config.keyField]));
        } else {
            selected = new Set();
        }
    }

    // Sorting handlers
    function handleSort(columnKey: string) {
        if (!config.enableSorting || !onSortChange) return;
        
        const column = config.columns.find(col => col.key === columnKey);
        if (!column?.sortable) return;
        
        let newDir: 'asc' | 'desc' = 'asc';
        if (sorting?.sortBy === columnKey) {
            newDir = sorting.sortDir === 'asc' ? 'desc' : 'asc';
        }
        
        onSortChange(columnKey, newDir);
    }

    function getAriaSort(columnKey: string): 'none' | 'ascending' | 'descending' {
        if (sorting?.sortBy !== columnKey) return 'none';
        return sorting.sortDir === 'asc' ? 'ascending' : 'descending';
    }

    // Action handlers
    function handleRowAction(action: string, key: number | string) {
        onRowAction?.({ action, id: key });
    }

    function handleBulkAction(action: string) {
        onBulkAction?.({ action, ids: Array.from(selected) });
    }

    // Format cell value
    function formatCell(column: DataTableConfig<T>['columns'][0], row: T): string {
        const value = row[column.key];
        if (column.formatter) {
            const formatted = column.formatter(value, row);
            return typeof formatted === 'string' ? formatted : '';
        }
        return value?.toString() ?? '';
    }
</script>

<!-- Bulk actions bar -->
{#if (config.enableSelection ?? true) && selected.size > 0 && (config.bulkActions?.length ?? 0) > 0}
    <div class="alert alert-light d-flex align-items-center justify-content-between py-2 mb-3">
        <div>
            <strong>{selected.size}</strong> {getMessage('selected')}
        </div>
        <div class="d-flex gap-2">
            {#each config.bulkActions ?? [] as action}
                <button
                    class="btn btn-sm btn-outline-primary"
                    onclick={() => handleBulkAction(action.id)}
                    aria-label={getMessage(action.label)}
                >
                    {#if action.icon}
                        <i class="bi {action.icon} me-1"></i>
                    {/if}
                    {getMessage(action.label)}
                </button>
            {/each}
        </div>
    </div>
{/if}

<!-- Table -->
<div class="d-flex flex-column flex-grow-1" style="min-height: 0;">
    <div class="table-responsive border rounded flex-grow-1" style="min-height: 0;">
        <table class="table table-sm table-hover align-middle mb-0">
        <thead class="table-light">
            <tr>
                <!-- Selection checkbox column -->
                {#if config.enableSelection ?? true}
                    <th style="width: 32px;">
                        <input
                            bind:this={selectAllEl}
                            type="checkbox"
                            class="form-check-input"
                            checked={allSelected}
                            onchange={(e) => toggleAll((e.target as HTMLInputElement).checked)}
                            aria-label="Select all rows"
                        />
                    </th>
                {/if}

                <!-- Data columns -->
                {#each config.columns as column}
                    <th
                        role="columnheader"
                        aria-sort={getAriaSort(column.key)}
                        class={column.headerClass ?? ''}
                        class:sortable={column.sortable && config.enableSorting}
                        style:width={column.width}
                        onclick={() => column.sortable ? handleSort(column.key) : null}
                    >
                        {getMessage(column.label)}
                        {#if column.sortable && config.enableSorting && sorting?.sortBy === column.key}
                            <i class="bi bi-chevron-{sorting.sortDir === 'asc' ? 'up' : 'down'} ms-1"></i>
                        {/if}
                    </th>
                {/each}

                <!-- Actions column -->
                {#if (config.actions?.length ?? 0) > 0}
                    <th class="text-center">{getMessage('actions')}</th>
                {/if}
            </tr>
        </thead>

        <tbody>
            {#if loading}
                <!-- Skeleton loader rows -->
                {#each Array(pagination?.pageSize ?? 10) as _}
                    <tr>
                        {#if config.enableSelection}
                            <td style="width: 40px;"><SkeletonLoader type="rect" width="16px" height="16px" /></td>
                        {/if}
                        {#each config.columns as column}
                            <td>
                                {#if column.key === 'ticketId' || column.key === 'userId' || column.key === 'categoryId'}
                                    <SkeletonLoader type="text" width="60px" />
                                {:else if column.key === 'status' || column.key === 'priority' || column.key === 'category' || column.key === 'role'}
                                    <SkeletonLoader type="rect" width="80px" height="24px" />
                                {:else if column.key === 'title' || column.key === 'name' || column.key === 'email'}
                                    <SkeletonLoader type="text" width="200px" />
                                {:else}
                                    <SkeletonLoader type="text" width="120px" />
                                {/if}
                            </td>
                        {/each}
                        {#if (config.actions?.length ?? 0) > 0}
                            <td class="text-center"><SkeletonLoader type="rect" width="60px" height="28px" /></td>
                        {/if}
                    </tr>
                {/each}
            {:else if error}
                <tr>
                    <td colspan="999" class="text-danger py-3 text-center">
                        <i class="bi bi-exclamation-triangle-fill me-2"></i>
                        {error}
                    </td>
                </tr>
            {:else if data.length === 0}
                <tr>
                    <td colspan="999" class="text-center py-4 text-muted">
                        <i class="bi bi-inbox me-2"></i>
                        {getMessage('no_results')}
                    </td>
                </tr>
            {:else}
                {#each data as row (row[config.keyField])}
                    <tr>
                        <!-- Selection checkbox -->
                        {#if config.enableSelection ?? true}
                            <td>
                                <input
                                    type="checkbox"
                                    class="form-check-input"
                                    checked={selected.has(row[config.keyField])}
                                    onchange={(e) => toggleRow(row[config.keyField], (e.target as HTMLInputElement).checked)}
                                    aria-label="Select row"
                                />
                            </td>
                        {/if}

                        <!-- Data cells -->
                        {#each config.columns as column}
                            <td class={column.cellClass ?? ''}>
                                {#if column.key === 'status' && column.formatter}
                                    {@const StatusBadge = column.formatter(row[column.key], row)}
                                    {#if typeof StatusBadge !== 'string'}
                                        <StatusBadge status={row[column.key]} />
                                    {:else}
                                        {StatusBadge}
                                    {/if}
                                {:else if column.key === 'priority' && column.formatter}
                                    {@const PriorityBadge = column.formatter(row[column.key], row)}
                                    {#if typeof PriorityBadge !== 'string'}
                                        <PriorityBadge priority={row[column.key]} />
                                    {:else}
                                        {PriorityBadge}
                                    {/if}
                                {:else if column.formatter}
                                    {@const formatted = column.formatter(row[column.key], row)}
                                    {#if typeof formatted === 'string'}
                                        <!-- Check if formatted value is an i18n key (starts with file_type_, category_, etc.) -->
                                        {@const translatedValue = formatted.startsWith('file_type_') || formatted.startsWith('category_') ? getMessage(formatted) : formatted}
                                        {translatedValue}
                                    {/if}
                                {:else}
                                    {formatCell(column, row)}
                                {/if}
                            </td>
                        {/each}

                        <!-- Action buttons -->
                        {#if (config.actions?.length ?? 0) > 0}
                            <td class="text-center actions-cell">
                                {#each config.actions ?? [] as action}
                                    {@const shouldShow = !action.conditional || action.conditional(row)}
                                    {#if shouldShow}
                                        {@const isToggle = action.id === 'toggle_active'}
                                        {@const isActive = row['isActive'] ?? false}
                                        {@const toggleIcon = isActive ? 'bi-toggle-on' : 'bi-toggle-off'}
                                        {@const toggleTitle = isActive ? 'action_deactivate_category' : 'action_activate_category'}
                                        <button
                                            class="btn btn-link btn-sm p-1 action-btn"
                                            onclick={() => handleRowAction(action.id, row[config.keyField])}
                                            title={getMessage(isToggle ? toggleTitle : action.label)}
                                            aria-label={getMessage(isToggle ? toggleTitle : action.label)}
                                        >
                                            <i class="bi {isToggle ? toggleIcon : action.icon}"></i>
                                        </button>
                                    {:else}
                                        <!-- Show N/A for conditional actions that are not available -->
                                        <span class="text-muted small px-2">
                                            {getMessage('preview_not_available')}
                                        </span>
                                    {/if}
                                {/each}
                            </td>
                        {/if}
                    </tr>
                {/each}
            {/if}
        </tbody>
        </table>
    </div>
</div>

<!-- Pagination -->
{#if pagination && onPageChange && onPageSizeChange}
    <div class="mt-3 mt-auto">
        <Pagination
            currentPage={pagination.currentPage}
            totalPages={pagination.totalPages}
            pageSize={pagination.pageSize}
            totalItems={pagination.totalItems}
            {onPageChange}
            {onPageSizeChange}
        />
    </div>
{/if}

<style>
    .sortable {
        cursor: pointer;
        user-select: none;
    }
    
    .sortable:hover {
        background-color: rgba(0, 0, 0, 0.05);
    }
    
    .table td,
    .table th {
        vertical-align: middle;
    }
    
    tbody tr {
        transition: background-color 0.15s ease-in-out;
    }

    /* Action buttons styling */
    .actions-cell {
        white-space: nowrap;
    }

    .action-btn {
        color: var(--ironpack-font-black);
        text-decoration: none;
        transition: color 0.15s ease-in-out;
        font-size: 1.1rem;
    }

    .action-btn:hover {
        color: var(--ironpack-red);
    }

    .action-btn i {
        pointer-events: none;
    }
</style>
