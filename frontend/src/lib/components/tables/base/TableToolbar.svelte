<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import Input from '$lib/components/ui/Input.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import DatePicker from '$lib/components/ui/DatePicker.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import { normalizeDateRange } from '$lib/utils/dateRange';

	type QuickRange = 'Custom' | 'Today' | 'Yesterday' | 'Last7' | 'Last30' | 'Last90';

	const QUICK_RANGE_ORDER: QuickRange[] = ['Custom', 'Today', 'Yesterday', 'Last7', 'Last30', 'Last90'];
	const QUICK_RANGE_LABEL_KEYS: Record<QuickRange, string> = {
		Custom: 'quick_range_custom',
		Today: 'quick_range_today',
		Yesterday: 'quick_range_yesterday',
		Last7: 'quick_range_last7',
		Last30: 'quick_range_last30',
		Last90: 'quick_range_last90'
	};

	let {
		enableSearch = true,
		enableFilters = true,
		enableExport = true,

		search = $bindable(''),
		status = $bindable<any>('All'),
		category = $bindable<any>('All'),
		priority = $bindable<any>('All'),
		dateFrom = $bindable<string | undefined>(undefined),
		dateTo = $bindable<string | undefined>(undefined),

		statusOptions = [],
		categoryOptions = [],
		priorityOptions = [],

		onExport
	}: {
		enableSearch?: boolean;
		enableFilters?: boolean;
		enableExport?: boolean;

		search?: string;
		status?: any;
		category?: any;
		priority?: any;
		dateFrom?: string;
		dateTo?: string;

		statusOptions?: Array<{ value: any; labelKey: string }>;
		categoryOptions?: Array<{ value: any; labelKey: string }>;
		priorityOptions?: Array<{ value: any; labelKey: string }>;

		onExport?: () => void;
	} = $props();

	let quickRange = $state<QuickRange>('Custom');
	let suppressQuickRangeEffect = $state(false);
	let suppressDateEffect = $state(false);
	let lastDateFieldChanged = $state<'from' | 'to' | null>(null);
	let previousFromValue = $state<string | undefined>(dateFrom);
	let previousToValue = $state<string | undefined>(dateTo);

	const quickRangeOptions = $derived(
		QUICK_RANGE_ORDER.map((value) => ({ value, labelKey: QUICK_RANGE_LABEL_KEYS[value] }))
	);

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

	function computeQuickRange(range: QuickRange): { from: string; to: string } | null {
		const today = startOfToday();
		switch (range) {
			case 'Today': {
				const formatted = formatDateValue(today);
				return { from: formatted, to: formatted };
			}
			case 'Yesterday': {
				const day = shiftDays(today, 1);
				const formatted = formatDateValue(day);
				return { from: formatted, to: formatted };
			}
			case 'Last7':
				return {
					from: formatDateValue(shiftDays(today, 7)),
					to: formatDateValue(today)
				};
			case 'Last30':
				return {
					from: formatDateValue(shiftDays(today, 30)),
					to: formatDateValue(today)
				};
			case 'Last90':
				return {
					from: formatDateValue(shiftDays(today, 90)),
					to: formatDateValue(today)
				};
			default:
				return null;
		}
	}

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
		dateFrom = computed.from;
		dateTo = computed.to;
	});

	$effect(() => {
		void dateFrom;
		void dateTo;
		if (suppressDateEffect) {
			suppressDateEffect = false;
			return;
		}
		if (quickRange !== 'Custom') {
			suppressQuickRangeEffect = true;
			quickRange = 'Custom';
		}
	});

	$effect(() => {
		const current = dateFrom;
		if (current !== previousFromValue) {
			previousFromValue = current;
			lastDateFieldChanged = 'from';
		}
	});

	$effect(() => {
		const current = dateTo;
		if (current !== previousToValue) {
			previousToValue = current;
			lastDateFieldChanged = 'to';
		}
	});

	$effect(() => {
		void dateFrom;
		void dateTo;
		void lastDateFieldChanged;
		const normalized = normalizeDateRange(dateFrom, dateTo, lastDateFieldChanged);
		if (normalized.from !== dateFrom) {
			dateFrom = normalized.from;
			return;
		}
		if (normalized.to !== dateTo) {
			dateTo = normalized.to;
		}
	});
</script>

<div class="d-flex gap-2 align-items-end">
	{#if enableSearch}
		<div class="me-2">
			<label for="table-search" class="form-label mb-1">{getMessage('search')}</label>
			<Input
				type="search"
				placeholder={getMessage('search_placeholder')}
				bind:value={search}
				id="table-search"
				ellipsis
			/>
		</div>
	{/if}

	{#if enableFilters}
		<div class="me-2">
			<label for="search-status" class="form-label mb-1">{getMessage('status')}</label>
			<Select bind:value={status} options={statusOptions} id="search-status" widthGroup="ticket-filters" />
		</div>

		<div class="me-2">
			<label for="search-category" class="form-label mb-1">{getMessage('category')}</label>
			<Select bind:value={category} options={categoryOptions} id="search-category" widthGroup="ticket-filters" />
		</div>

		<div class="me-2">
			<label for="search-priority" class="form-label mb-1">{getMessage('priority')}</label>
			<Select bind:value={priority} options={priorityOptions} id="search-priority" widthGroup="ticket-filters" />
		</div>

		<div class="me-2">
			<label for="search-date-from" class="form-label mb-1">{getMessage('date_from')}</label>
			<DatePicker bind:value={dateFrom} id="search-date-from" />
		</div>
		<div class="me-2">
			<label for="search-date-to" class="form-label mb-1">{getMessage('date_to')}</label>
			<DatePicker bind:value={dateTo} id="search-date-to" />
		</div>

		<div class="me-2">
			<label for="search-quick-range" class="form-label mb-1">{getMessage('quick_range')}</label>
			<Select
				id="search-quick-range"
				bind:value={quickRange}
				options={quickRangeOptions}
				widthGroup="ticket-filters"
			/>
		</div>
	{/if}

	<div class="ms-auto d-flex gap-2">
		{#if enableExport && onExport}
			<Button type="button" variant="outline-secondary" size="sm" onclick={onExport}>
				<i class="bi bi-download me-1"></i>
				{getMessage('export_csv')}
			</Button>
		{/if}
	</div>
</div>
