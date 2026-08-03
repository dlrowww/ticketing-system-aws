<script lang="ts">
    import { getMessage } from '$lib/i18n';
    import Button from '$lib/components/ui/Button.svelte';
    import Select from '$lib/components/ui/Select.svelte';

    let {
        currentPage,
        totalPages,
        pageSize,
        totalItems,
        onPageChange,
        onPageSizeChange
    }: {
        currentPage: number;
        totalPages: number;
        pageSize: number;
        totalItems: number;
        onPageChange: (page: number) => void;
        onPageSizeChange: (size: number) => void;
    } = $props();

    // Calculate display range
    const from = $derived((currentPage - 1) * pageSize + 1);
    const to = $derived(Math.min(currentPage * pageSize, totalItems));
    
    // Pagination button states
    const isFirstPage = $derived(currentPage <= 1);
    const isLastPage = $derived(currentPage >= totalPages);
    
    // Local state for page size select
    let selectedPageSize = $state(pageSize);
    
    // Sync when prop changes from parent
    $effect(() => {
        selectedPageSize = pageSize;
    });
    
    // Watch for changes and call callback
    $effect(() => {
        if (selectedPageSize !== pageSize) {
            onPageSizeChange(selectedPageSize);
        }
    });
    
    function handlePageChange(page: number) {
        if (page < 1 || page > totalPages) return;
        onPageChange(page);
    }
</script>

<div class="d-flex flex-wrap align-items-center justify-content-between gap-3 mt-3">
    <!-- Rows per page selector -->
    <div class="d-flex align-items-center gap-2">
        <span class="text-nowrap">{getMessage('rows_per_page')}:</span>
        <Select
            bind:value={selectedPageSize}
            options={[
                { value: 10, labelKey: '10', label: '10' },
                { value: 25, labelKey: '25', label: '25' },
                { value: 50, labelKey: '50', label: '50' },
                { value: 100, labelKey: '100', label: '100' }
            ]}
            class="form-select-sm"
            style="width: auto;"
        />
    </div>
    
    <!-- Display current range -->
    <div class="text-muted text-nowrap">
        {from}-{to} {getMessage('total')} {totalItems}
    </div>

    <!-- Page navigation -->
    <nav aria-label="Table pagination">
        <div class="btn-group">
            <!-- First page -->
            <Button
                variant="outline-secondary"
                size="sm"
                onclick={() => handlePageChange(1)}
                disabled={isFirstPage}
            >
                <i class="bi bi-chevron-double-left"></i>
            </Button>
            
            <!-- Previous page -->
            <Button
                variant="outline-secondary"
                size="sm"
                onclick={() => handlePageChange(currentPage - 1)}
                disabled={isFirstPage}
            >
                <i class="bi bi-chevron-left"></i>
            </Button>
            
            <!-- Current page indicator -->
            <span class="btn btn-light btn-sm disabled">
                {currentPage} / {totalPages}
            </span>
            
            <!-- Next page -->
            <Button
                variant="outline-secondary"
                size="sm"
                onclick={() => handlePageChange(currentPage + 1)}
                disabled={isLastPage}
            >
                <i class="bi bi-chevron-right"></i>
            </Button>
            
            <!-- Last page -->
            <Button
                variant="outline-secondary"
                size="sm"
                onclick={() => handlePageChange(totalPages)}
                disabled={isLastPage}
            >
                <i class="bi bi-chevron-double-right"></i>
            </Button>
        </div>
    </nav>
</div>

<style>
    .btn-group {
        flex-wrap: nowrap;
    }
</style>
