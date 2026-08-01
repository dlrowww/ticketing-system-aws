<script lang="ts">
    import type { ChartConfiguration } from 'chart.js';
    import type { PageData } from './$types';

    import { onMount } from 'svelte';
    import { goto, invalidateAll } from '$app/navigation';
    import { getMessage } from '$lib/i18n';
    import { DatePicker, Select, Button } from '$lib/components/ui';
    import { lookups } from '$lib/lookups/Lookups';
    import { getCategoryName } from '$lib/stores/categories';
    import ChartCanvas from '$lib/components/charts/ChartCanvas.svelte';
    import { getChartPalette } from '$lib/components/charts/chartTheme';
    import { getStatusBackground, getStatusBorderColor } from '$lib/theme/statusPalette';
    import { getPriorityBackground, getPriorityBorderColor } from '$lib/theme/priorityPalette';
    import { formatDateForTrendAxis, formatDateForFiltersSubtitle } from '$lib/utils/dateTime';
    import { normalizeDateRange } from '$lib/utils/dateRange';
    import { UserRole } from '$lib/types/enums';
    import { page } from '$app/stores';

    let { data }: { data: PageData } = $props();

    // Get user from parent layout
    const user = $derived($page.data.user);
    const isAdmin = $derived(Number(user?.roleId) === UserRole.Admin);

    let palette = $state<ReturnType<typeof getChartPalette> | null>(null);

    onMount(() => {
        palette = getChartPalette();
    });

    type DashboardQuickRange = 'Custom' | 'Last30' | 'Last90' | 'Last180' | 'Last365';

    const DASHBOARD_QUICK_RANGE_ORDER: DashboardQuickRange[] = ['Custom', 'Last30', 'Last90', 'Last180', 'Last365'];
    const DASHBOARD_QUICK_RANGE_LABEL_KEYS: Record<DashboardQuickRange, string> = {
        Custom: 'quick_range_custom',
        Last30: 'quick_range_last30',
        Last90: 'quick_range_last90',
        Last180: 'quick_range_last180',
        Last365: 'quick_range_last365'
    };

    const quickRangeOptions = DASHBOARD_QUICK_RANGE_ORDER.map((value) => ({
        value,
        labelKey: DASHBOARD_QUICK_RANGE_LABEL_KEYS[value]
    }));

    let formDateFrom = $state<string | undefined>(data.filters.from ?? undefined);
    let formDateTo = $state<string | undefined>(data.filters.to ?? undefined);
    let formDays = $state<number>(data.filters.days);

    function startOfToday(): Date {
        const now = new Date();
        return new Date(now.getFullYear(), now.getMonth(), now.getDate());
    }

    function shiftDays(base: Date, days: number): Date {
        const copy = new Date(base);
        copy.setDate(copy.getDate() - days);
        return copy;
    }

    function formatDateValue(date: Date): string {
        const year = date.getFullYear();
        const month = String(date.getMonth() + 1).padStart(2, '0');
        const day = String(date.getDate()).padStart(2, '0');
        return `${year}-${month}-${day}`;
    }

    function computeQuickRange(range: DashboardQuickRange): { from: string; to: string; days: number } | null {
        const today = startOfToday();
        switch (range) {
            case 'Last30':
                return {
                    from: formatDateValue(shiftDays(today, 30)),
                    to: formatDateValue(today),
                    days: 30
                };
            case 'Last90':
                return {
                    from: formatDateValue(shiftDays(today, 90)),
                    to: formatDateValue(today),
                    days: 90
                };
            case 'Last180':
                return {
                    from: formatDateValue(shiftDays(today, 180)),
                    to: formatDateValue(today),
                    days: 180
                };
            case 'Last365':
                return {
                    from: formatDateValue(shiftDays(today, 365)),
                    to: formatDateValue(today),
                    days: 365
                };
            default:
                return null;
        }
    }

    function detectInitialQuickRange(): DashboardQuickRange {
        if (formDateFrom && formDateTo) {
            for (const range of DASHBOARD_QUICK_RANGE_ORDER) {
                if (range === 'Custom') continue;
                const computed = computeQuickRange(range);
                if (computed && computed.from === formDateFrom && computed.to === formDateTo) {
                    return range;
                }
            }
        }
        return 'Custom';
    }

    function computeCustomDays(fromValue?: string, toValue?: string): number | null {
        if (!fromValue || !toValue) return null;
        const fromDate = new Date(`${fromValue}T00:00:00`);
        const toDate = new Date(`${toValue}T00:00:00`);
        if (Number.isNaN(fromDate.getTime()) || Number.isNaN(toDate.getTime())) return null;
        const diffMs = Math.abs(toDate.getTime() - fromDate.getTime());
        const diffDays = Math.floor(diffMs / (24 * 60 * 60 * 1000)) + 1;
        const clamped = Math.min(Math.max(diffDays, 1), 365);
        return clamped;
    }

    let quickRange = $state<DashboardQuickRange>(detectInitialQuickRange());
    let suppressQuickRangeEffect = $state(false);
    let suppressDateEffect = $state(false);

    let lastDateFieldChanged = $state<'from' | 'to' | null>(null);
    let previousFromValue = $state<string | undefined>(formDateFrom);
    let previousToValue = $state<string | undefined>(formDateTo);

    $effect(() => {
        const range = quickRange;
        if (suppressQuickRangeEffect) {
            suppressQuickRangeEffect = false;
            return;
        }
        if (range === 'Custom') {
            return;
        }
        const computed = computeQuickRange(range);
        if (!computed) {
            return;
        }
        suppressDateEffect = true;
        formDateFrom = computed.from;
        formDateTo = computed.to;
        formDays = computed.days;
    });

    $effect(() => {
        void formDateFrom;
        void formDateTo;
        if (suppressDateEffect) {
            suppressDateEffect = false;
            return;
        }
        if (quickRange !== 'Custom') {
            suppressQuickRangeEffect = true;
            quickRange = 'Custom';
        }
        if (quickRange === 'Custom') {
            const customDays = computeCustomDays(formDateFrom, formDateTo);
            if (customDays !== null) {
                formDays = customDays;
            }
        }
    });

    $effect(() => {
        const current = formDateFrom;
        if (current !== previousFromValue) {
            previousFromValue = current;
            lastDateFieldChanged = 'from';
        }
    });

    $effect(() => {
        const current = formDateTo;
        if (current !== previousToValue) {
            previousToValue = current;
            lastDateFieldChanged = 'to';
        }
    });

    $effect(() => {
        void formDateFrom;
        void formDateTo;
        void lastDateFieldChanged;
        const normalized = normalizeDateRange(formDateFrom, formDateTo, lastDateFieldChanged);
        if (normalized.from !== formDateFrom) {
            formDateFrom = normalized.from;
            return;
        }
        if (normalized.to !== formDateTo) {
            formDateTo = normalized.to;
        }
    });

    // Create a unique key that changes when data changes to force chart re-rendering
    const chartKey = $derived(JSON.stringify({
        byStatus: data.byStatus,
        byCategory: data.byCategory,
        trend: data.trend,
        stats: data.stats
    }));

    // Get all possible categories from lookups
    const allCategories = $derived(lookups.category());
    
    // Build complete category data (all categories, with 0 for missing ones)
    const completeCategoryData = $derived.by(() => {
        if (!data.byCategory || !Array.isArray(data.byCategory)) {
            return allCategories.map(cat => ({
                categoryId: cat.categoryId,
                categoryName: getCategoryName(cat.categoryId),
                count: 0
            }));
        }
        return allCategories.map(cat => {
            const found = data.byCategory.find(r => r.categoryId === cat.categoryId);
            return {
                categoryId: cat.categoryId,
                categoryName: getCategoryName(cat.categoryId),
                count: found?.count ?? 0
            };
        });
    });

    const categoryLabels = $derived.by(() => completeCategoryData.map(r => r.categoryName));
    const categoryCounts = $derived.by(() => completeCategoryData.map(r => r.count));
    const categoryColors = $derived.by(() =>
        categoryLabels.map((_, index) => pastelColors[index % pastelColors.length])
    );

    // Helper to detect if category data is empty (all zeros)
    const hasCategoryData = $derived(categoryCounts.some(count => count > 0));

    const statusCountMap = $derived.by(() => {
        const map = new Map<number, number>();
        if (data.byStatus && Array.isArray(data.byStatus)) {
            for (const row of data.byStatus) {
                map.set(row.status, row.count);
            }
        }
        return map;
    });

    const statusSeries = $derived.by(() => {
        const statuses = lookups.ticketStatus();
        return statuses.map((item) => {
            const status = item.id;
            const labelKey = item.name ?? 'not_available';
            const count = statusCountMap.get(status) ?? 0;
            const color = getStatusBackground(status);
            const borderColor = getStatusBorderColor(status);
            return { labelKey, count, color, borderColor };
        });
    });

    const statusLabels = $derived.by(() => statusSeries.map((g) => getMessage(g.labelKey)));
    const statusCounts = $derived.by(() => statusSeries.map((g) => g.count));
    const statusColors = $derived.by(() => statusSeries.map((g) => g.color));
    const statusBorderColors = $derived.by(() => statusSeries.map((g) => g.borderColor));

    // Helper to detect if status data is empty (all zeros)
    const hasStatusData = $derived(statusCounts.some(count => count > 0));

    // Priority chart data (for Team Leaders)
    const priorityCountMap = $derived.by(() => {
        const map = new Map<number, number>();
        if (data.byPriority && Array.isArray(data.byPriority)) {
            for (const row of data.byPriority) {
                map.set(row.priority, row.count);
            }
        }
        return map;
    });

    const prioritySeries = $derived.by(() => {
        const priorities = lookups.priority();
        return priorities.map((item) => {
            const priority = item.id;
            const labelKey = item.name ?? 'not_available';
            const count = priorityCountMap.get(priority) ?? 0;
            const color = getPriorityBackground(priority);
            const borderColor = getPriorityBorderColor(priority);
            return { labelKey, count, color, borderColor };
        });
    });

    const priorityLabels = $derived.by(() => prioritySeries.map((g) => getMessage(g.labelKey)));
    const priorityCounts = $derived.by(() => prioritySeries.map((g) => g.count));
    const priorityColors = $derived.by(() => prioritySeries.map((g) => g.color));
    const priorityBorderColors = $derived.by(() => prioritySeries.map((g) => g.borderColor));

    // Helper to detect if priority data is empty (all zeros)
    const hasPriorityData = $derived(priorityCounts.some(count => count > 0));

    const trendLabels = $derived.by(() => {
        if (!data.trend || !Array.isArray(data.trend)) return [];
        return data.trend.map((p) => formatDateForTrendAxis(p.date));
    });
    const trendCounts = $derived.by(() => {
        if (!data.trend || !Array.isArray(data.trend)) return [];
        return data.trend.map((p) => p.count);
    });

    // Helper to detect if trend data is empty (all zeros or no data)
    const hasTrendData = $derived.by(() => {
        if (!data.trend || !Array.isArray(data.trend)) return false;
        return data.trend.length > 0 && trendCounts.some(count => count > 0);
    });

    const fallbackPastels = ['#a8d5e2', '#ffd4a3', '#c9b8d8', '#b4e7ce', '#ffb6c1', '#f7dc6f', '#d4a5a5'];
    const pastelColors = $derived.by(() => {
        const colors = palette?.pastelSeries;
        return colors && colors.length > 0 ? colors : fallbackPastels;
    });
    const ticketTrendColor = $derived.by(() => palette?.ticketTrendColor ?? '');

    const filtersSummary = $derived.by(() => {
        const currentRange = quickRange;
        const fromValue = formDateFrom;
        const toValue = formDateTo;
        const hasFrom = Boolean(fromValue);
        const hasTo = Boolean(toValue);

        if (currentRange !== 'Custom') {
            const labelKey = DASHBOARD_QUICK_RANGE_LABEL_KEYS[currentRange];
            return getMessage(labelKey);
        }

        if (!hasFrom && !hasTo) {
            return getMessage('dashboard_filters_all_time');
        }

        const formattedFrom = hasFrom ? formatDateForFiltersSubtitle(fromValue) : '';
        const formattedTo = hasTo ? formatDateForFiltersSubtitle(toValue) : '';

        if (hasFrom && hasTo) {
            return getMessage('dashboard_filters_range', { from: formattedFrom, to: formattedTo });
        }

        if (hasFrom) {
            return getMessage('dashboard_filters_from', { from: formattedFrom });
        }

        if (hasTo) {
            return getMessage('dashboard_filters_to', { to: formattedTo });
        }

        return getMessage('dashboard_filters_all_time');
    });

    const byStatusChart = $derived.by((): ChartConfiguration<'doughnut'> => {
  return {
    type: 'doughnut',
    data: {
      labels: statusLabels,
      datasets: [
        {
          label: getMessage('dashboard_chart_count'),
          data: statusCounts,
          backgroundColor: statusColors,

          borderColor: statusBorderColors,
          borderWidth: 1,
          borderAlign: 'inner',

          // Optional polish:
          spacing: 1,
          hoverBorderWidth: 2
        }
      ]
    },
    options: {
      responsive: true,
      maintainAspectRatio: false,
      rotation: -90,
      circumference: 180,
      radius: '72%',
      cutout: '62%',
      layout: {
        padding: { top: 6, right: 8, bottom: 0, left: 8 }
      },
      plugins: {
        legend: {
          position: 'right',
          labels: {
            usePointStyle: true,
            boxWidth: 10,
            padding: 12
          }
        }
      }
    }
  };
});

    const byCategoryChart = $derived.by((): ChartConfiguration<'bar'> => {
        const maxCount = Math.max(...categoryCounts, 1);
        const suggestedMax = Math.ceil(maxCount * 1.1); // Add 10% padding to max value
        
        return {
            type: 'bar',
            data: {
                labels: categoryLabels,
                datasets: [
                    {
                        label: getMessage('dashboard_chart_count'),
                        data: categoryCounts,
                        backgroundColor: categoryColors
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y', // Horizontal bars scale better with many categories
                plugins: {
                    legend: {
                        display: true,
                        position: 'right',
                        labels: {
                            usePointStyle: true,
                            boxWidth: 10,
                            padding: 12,
                            generateLabels(chart) {
                                const dataset = chart.data.datasets[0];
                                const background = Array.isArray(dataset.backgroundColor)
                                    ? dataset.backgroundColor
                                    : [];
                                return chart.data.labels.map((text, index) => ({
                                    text,
                                    fillStyle: background[index] ?? pastelColors[index % pastelColors.length],
                                    strokeStyle: background[index] ?? pastelColors[index % pastelColors.length],
                                    hidden: false,
                                    index,
                                    datasetIndex: 0
                                }));
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: false,
                        beginAtZero: true,
                        suggestedMax: suggestedMax,
                        ticks: {
                            precision: 0 // Only show whole numbers
                        }
                    },
                    y: {
                        stacked: false,
                        grid: {
                            display: false
                        }
                    }
                }
            }
        };
    });

    const byPriorityChart = $derived.by((): ChartConfiguration<'bar'> => {
        const maxCount = Math.max(...priorityCounts, 1);
        const suggestedMax = Math.ceil(maxCount * 1.1);
        
        return {
            type: 'bar',
            data: {
                labels: priorityLabels,
                datasets: [
                    {
                        label: getMessage('dashboard_chart_count'),
                        data: priorityCounts,
                        backgroundColor: priorityColors,
                        borderColor: priorityBorderColors,
                        borderWidth: 1
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                indexAxis: 'y',
                plugins: {
                    legend: {
                        display: true,
                        position: 'right',
                        labels: {
                            usePointStyle: true,
                            boxWidth: 10,
                            padding: 12,
                            generateLabels(chart) {
                                const dataset = chart.data.datasets[0];
                                const background = Array.isArray(dataset.backgroundColor)
                                    ? dataset.backgroundColor
                                    : [];
                                return chart.data.labels.map((text, index) => ({
                                    text,
                                    fillStyle: background[index] ?? '#e9ecef',
                                    strokeStyle: background[index] ?? '#e9ecef',
                                    hidden: false,
                                    index,
                                    datasetIndex: 0
                                }));
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        stacked: false,
                        beginAtZero: true,
                        suggestedMax: suggestedMax,
                        ticks: {
                            precision: 0
                        }
                    },
                    y: {
                        stacked: false,
                        grid: {
                            display: false
                        }
                    }
                }
            }
        };
    });

    const trendChart = $derived.by((): ChartConfiguration<'line'> => {
        // Use larger points when there's limited data (single day or very few days)
        const dataPointCount = trendCounts.length;
        const pointRadius = dataPointCount <= 1 ? 6 : dataPointCount <= 3 ? 4 : 3;
        const pointHoverRadius = dataPointCount <= 1 ? 8 : dataPointCount <= 3 ? 6 : 5;
        
        return {
            type: 'line',
            data: {
                labels: trendLabels,
                datasets: [
                    {
                        label: getMessage('dashboard_chart_count'),
                        data: trendCounts,
                        borderColor: ticketTrendColor,
                        backgroundColor: ticketTrendColor,
                        tension: 0.2,
                        pointRadius: pointRadius,
                        pointHoverRadius: pointHoverRadius,
                        pointBackgroundColor: ticketTrendColor,
                        pointBorderColor: '#fff',
                        pointBorderWidth: 2
                    }
                ]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: { display: false }
                },
                scales: {
                    y: {
                        title: {
                            display: true,
                            text: getMessage('dashboard_axis_tickets')
                        },
                        ticks: {
                            precision: 0 // optional: keep whole numbers
                        },
                        beginAtZero: true // optional but usually correct for counts
                    },
                    x: {
                        title: {
                            display: false
                        }
                    }
                }
            },
        };
    });

    async function handleApplyFilters(event: Event) {
        event.preventDefault();
        
        const params = new URLSearchParams();
        if (formDateFrom && formDateFrom.trim()) params.set('from', formDateFrom.trim());
        if (formDateTo && formDateTo.trim()) params.set('to', formDateTo.trim());
        params.set('days', String(formDays));
        
        // Use SvelteKit's goto with invalidateAll to refresh data without full page reload
        await goto(`/app/dashboard?${params.toString()}`, { 
            invalidateAll: true,
            replaceState: false
        });
    }
</script>

<div class="container-fluid p-0">
    <!-- Header + filters -->
    <div class="d-flex align-items-start justify-content-between flex-wrap gap-3 mb-2">
        <div>
            <div class="d-flex align-items-center gap-2">
                <i class="bi bi-graph-up"></i>
                <h1 class="h4 m-0">{getMessage('dashboard_title')}</h1>
            </div>
            <div class="text-muted small mt-1">{filtersSummary}</div>
        </div>

        <form onsubmit={handleApplyFilters} class="d-flex align-items-end gap-2 flex-wrap">
            <div>
                <label class="form-label mb-1" for="dashboard-date-from">{getMessage('date_from')}</label>
                <DatePicker id="dashboard-date-from" name="from" bind:value={formDateFrom} />
            </div>
            <div>
                <label class="form-label mb-1" for="dashboard-date-to">{getMessage('date_to')}</label>
                <DatePicker id="dashboard-date-to" name="to" bind:value={formDateTo} />
            </div>
            <div>
                <label class="form-label mb-1" for="dashboard-quick-range">{getMessage('quick_range')}</label>
                <Select id="dashboard-quick-range" bind:value={quickRange} options={quickRangeOptions} />
            </div>
            <input type="hidden" name="days" value={formDays} />
            <Button type="submit" variant="primary">
                <i class="bi bi-funnel me-2"></i>
                {getMessage('dashboard_apply_filters')}
            </Button>
        </form>
    </div>

    <!-- KPIs -->
    <div class="row g-2 mb-2">
        <div class="col-12 col-md-6 col-lg-3">
            <div class="card h-100">
                <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <div class="text-muted small">{getMessage('dashboard_kpi_total')}</div>
                            <div class="fs-5 fw-semibold">{data.stats.totalTickets}</div>
                        </div>
                        <i class="bi bi-ticket-perforated fs-5 text-secondary"></i>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-12 col-md-6 col-lg-3">
            <div class="card h-100">
                <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <div class="text-muted small">{getMessage('dashboard_kpi_open')}</div>
                            <div class="fs-5 fw-semibold">{data.stats.openTickets}</div>
                        </div>
                        <i class="bi bi-folder2-open fs-5 text-secondary"></i>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-12 col-md-6 col-lg-3">
            <div class="card h-100">
                <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <div class="text-muted small">{getMessage('dashboard_kpi_in_progress')}</div>
                            <div class="fs-5 fw-semibold">{data.stats.inProgressTickets}</div>
                        </div>
                        <i class="bi bi-hourglass-split fs-5 text-secondary"></i>
                    </div>
                </div>
            </div>
        </div>

        <div class="col-12 col-md-6 col-lg-3">
            <div class="card h-100">
                <div class="card-body py-2 px-3">
                    <div class="d-flex justify-content-between align-items-start">
                        <div>
                            <div class="text-muted small">{getMessage('dashboard_kpi_resolved')}</div>
                            <div class="fs-5 fw-semibold">{data.stats.resolvedTickets}</div>
                        </div>
                        <i class="bi bi-check2-circle fs-5 text-secondary"></i>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <!-- Primary charts -->
    <div class="row g-2">
        <div class="col-12 col-lg-6">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center gap-2">
                        <i class="bi bi-pie-chart"></i>
                        <span class="fw-semibold">{getMessage('dashboard_tickets_by_status')}</span>
                    </div>
                </div>
                <div class="card-body">
                    {#if !hasStatusData}
                        <div class="d-flex flex-column align-items-center justify-content-center" style="height: 200px;">
                            <i class="bi bi-pie-chart text-muted" style="font-size: 3rem;"></i>
                            <div class="text-muted mt-3">{getMessage('no_data_for_period')}</div>
                            <div class="text-muted small">{getMessage('try_different_date_range')}</div>
                        </div>
                    {:else}
                        {#key chartKey}
                            <div style="height: 200px;">
                                <ChartCanvas ariaLabel={getMessage('dashboard_chart_pie')} config={byStatusChart} />
                            </div>
                        {/key}
                    {/if}
                </div>
            </div>
        </div>

        <div class="col-12 col-lg-6">
            <div class="card h-100">
                <div class="card-header d-flex align-items-center justify-content-between">
                    <div class="d-flex align-items-center gap-2">
                        <i class="bi bi-bar-chart"></i>
                        <span class="fw-semibold">
                            {getMessage(isAdmin ? 'dashboard_tickets_by_category' : 'dashboard_tickets_by_priority')}
                        </span>
                    </div>
                </div>
                <div class="card-body">
                    {#if isAdmin}
                        {#if !hasCategoryData}
                            <div class="d-flex flex-column align-items-center justify-content-center" style="height: 200px;">
                                <i class="bi bi-bar-chart text-muted" style="font-size: 3rem;"></i>
                                <div class="text-muted mt-3">{getMessage('no_data_for_period')}</div>
                                <div class="text-muted small">{getMessage('try_different_date_range')}</div>
                            </div>
                        {:else}
                            {#key chartKey}
                                <div style="height: 200px;">
                                    <ChartCanvas ariaLabel={getMessage('dashboard_chart_bar')} config={byCategoryChart} />
                                </div>
                            {/key}
                        {/if}
                    {:else}
                        {#if !hasPriorityData}
                            <div class="d-flex flex-column align-items-center justify-content-center" style="height: 200px;">
                                <i class="bi bi-bar-chart text-muted" style="font-size: 3rem;"></i>
                                <div class="text-muted mt-3">{getMessage('no_data_for_period')}</div>
                                <div class="text-muted small">{getMessage('try_different_date_range')}</div>
                            </div>
                        {:else}
                            {#key chartKey}
                                <div style="height: 200px;">
                                    <ChartCanvas ariaLabel={getMessage('dashboard_chart_bar')} config={byPriorityChart} />
                                </div>
                            {/key}
                        {/if}
                    {/if}
                </div>
            </div>
        </div>
    </div>

    <!-- Trend (full width) -->
    <div class="card mt-2">
        <div class="card-header d-flex align-items-center justify-content-between flex-wrap gap-2">
            <div class="d-flex align-items-center gap-2">
                <i class="bi bi-activity"></i>
                <span class="fw-semibold">{getMessage('dashboard_ticket_trend')}</span>
            </div>
            <div class="text-muted small">{getMessage(data.filters.days === 1 ? 'dashboard_last_day' : 'dashboard_last_days', { days: data.filters.days })}</div>
        </div>
        <div class="card-body">
            {#if !hasTrendData}
                <div class="d-flex flex-column align-items-center justify-content-center" style="height: 200px;">
                    <i class="bi bi-activity text-muted" style="font-size: 3rem;"></i>
                    <div class="text-muted mt-3">{getMessage('no_data_for_period')}</div>
                    <div class="text-muted small">{getMessage('try_different_date_range')}</div>
                </div>
            {:else}
                {#key chartKey}
                    <div style="height: 200px;">
                        <ChartCanvas ariaLabel={getMessage('dashboard_chart_line')} config={trendChart} />
                    </div>
                {/key}
            {/if}
        </div>
        <div class="card-footer">
            <div class="d-flex align-items-center gap-2">
                <i class="bi bi-stopwatch text-secondary"></i>
                <span class="text-muted">{getMessage('dashboard_avg_resolution_time')}</span>
                <span class="fw-semibold">{data.stats.averageResolutionTimeHours.toFixed(1)}</span>
                <span class="text-muted">{getMessage('dashboard_hours')}</span>
            </div>
        </div>
    </div>
</div>