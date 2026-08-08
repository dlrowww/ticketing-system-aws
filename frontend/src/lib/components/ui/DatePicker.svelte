<script lang="ts">
	/**
	 * Custom DatePicker component with IronPack styling
	 *
	 * Fully custom calendar dropdown styled to match Select.svelte with IronPack red colors,
	 * NavRail hover/focus/selected states, and a calendar icon.
	 *
	 * @example
	 * <DatePicker bind:value={dateFrom} placeholder="Select date" />
	 * <DatePicker bind:value={dateTo} error="Date is required" min="2025-01-01" />
	 */

	import { getMessage } from '$lib/i18n';
	import { formatDateForPicker, formatMonthYearHeading } from '$lib/utils/dateTime';

	interface DatePickerProps {
		value?: string | undefined;
		placeholder?: string;
		disabled?: boolean;
		readonly?: boolean;
		required?: boolean;
		class?: string;
		style?: string;
		error?: string | string[];
		id?: string;
		name?: string;
		min?: string;
		max?: string;
	}

	let {
		value = $bindable<string | undefined>(undefined),
		placeholder,
		disabled = false,
		readonly = false,
		required = false,
		class: className = '',
		style: styleAttr,
		error,
		id,
		name,
		min,
		max
	}: DatePickerProps = $props();

	let open = $state(false);
	let currentMonth = $state(new Date());

	// Parse value to Date or use today
	const selectedDate = $derived.by(() => {
		if (!value) return null;
		const parsed = new Date(value + 'T00:00:00');
		return isNaN(parsed.getTime()) ? null : parsed;
	});

	const displayText = $derived.by(() => {
		if (!selectedDate) return placeholder ?? getMessage('select_date');
		return formatDateForPicker(selectedDate);
	});

	const showPlaceholder = $derived(!value);

	function formatDate(date: Date): string {
		const year = date.getFullYear();
		const month = String(date.getMonth() + 1).padStart(2, '0');
		const day = String(date.getDate()).padStart(2, '0');
		return `${year}-${month}-${day}`;
	}

	function toggleOpen() {
		if (disabled || readonly) return;
		open = !open;
		if (open && selectedDate) {
			currentMonth = new Date(selectedDate);
		} else if (open) {
			currentMonth = new Date();
		}
	}

	function close() {
		open = false;
	}

	function selectDate(date: Date) {
		const formatted = formatDate(date);

		// Check min/max constraints
		if (min && formatted < min) return;
		if (max && formatted > max) return;

		value = formatted;
		close();
	}

	function clearDate() {
		value = undefined;
		close();
	}

	function selectToday() {
		selectDate(new Date());
	}

	function previousMonth() {
		currentMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() - 1, 1);
	}

	function nextMonth() {
		currentMonth = new Date(currentMonth.getFullYear(), currentMonth.getMonth() + 1, 1);
	}

	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('.date-picker-container')) {
			close();
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (disabled || readonly) return;

		if (event.key === 'Escape') {
			close();
			return;
		}

		if (event.key === 'Enter' || event.key === ' ') {
			event.preventDefault();
			toggleOpen();
		}
	}

	$effect(() => {
		if (open) {
			document.addEventListener('click', handleClickOutside);
			return () => document.removeEventListener('click', handleClickOutside);
		}
	});

	// Generate calendar days
	const calendarDays = $derived.by(() => {
		const year = currentMonth.getFullYear();
		const month = currentMonth.getMonth();

		const firstDay = new Date(year, month, 1);
		const lastDay = new Date(year, month + 1, 0);

		const startPadding = firstDay.getDay(); // 0 = Sunday
		const days: (Date | null)[] = [];

		// Add padding for days before month starts
		for (let i = 0; i < startPadding; i++) {
			days.push(null);
		}

		// Add all days of the month
		for (let day = 1; day <= lastDay.getDate(); day++) {
			days.push(new Date(year, month, day));
		}

		return days;
	});

	const monthYearDisplay = $derived.by(() => formatMonthYearHeading(currentMonth));

	function isSameDay(date1: Date | null, date2: Date | null): boolean {
		if (!date1 || !date2) return false;
		return (
			date1.getFullYear() === date2.getFullYear() &&
			date1.getMonth() === date2.getMonth() &&
			date1.getDate() === date2.getDate()
		);
	}

	function isToday(date: Date | null): boolean {
		if (!date) return false;
		const today = new Date();
		return isSameDay(date, today);
	}

	function isDisabled(date: Date | null): boolean {
		if (!date) return true;
		const formatted = formatDate(date);
		if (min && formatted < min) return true;
		if (max && formatted > max) return true;
		return false;
	}

	const inputClass = $derived(
		['date-picker-input', error ? 'is-invalid' : '', className].filter(Boolean).join(' ')
	);

	const containerStyle = $derived(styleAttr ?? '');
</script>

<div class="date-picker-container" style={containerStyle}>
	<button
		type="button"
		{id}
		class={inputClass}
		{disabled}
		aria-describedby={error && id ? `${id}-error` : undefined}
		aria-haspopup="dialog"
		aria-expanded={open}
		onclick={toggleOpen}
		onkeydown={handleKeydown}
	>
		<span class="date-picker-text" class:placeholder={showPlaceholder}>
			{displayText}
		</span>
		<i class="bi bi-calendar3 calendar-icon"></i>
	</button>

	{#if name}
		<input type="hidden" {name} value={value ?? ''} />
	{/if}

	<div class="calendar-dropdown" class:open role="dialog" aria-hidden={!open}>
		<div class="calendar-header">
			<button
				type="button"
				class="calendar-nav"
				onclick={previousMonth}
				aria-label="Previous month"
			>
				<i class="bi bi-chevron-left"></i>
			</button>
			<div class="calendar-month-year">{monthYearDisplay}</div>
			<button type="button" class="calendar-nav" onclick={nextMonth} aria-label="Next month">
				<i class="bi bi-chevron-right"></i>
			</button>
		</div>

		<div class="calendar-weekdays">
			<div class="calendar-weekday">Su</div>
			<div class="calendar-weekday">Mo</div>
			<div class="calendar-weekday">Tu</div>
			<div class="calendar-weekday">We</div>
			<div class="calendar-weekday">Th</div>
			<div class="calendar-weekday">Fr</div>
			<div class="calendar-weekday">Sa</div>
		</div>

		<div class="calendar-days">
			{#each calendarDays as day}
				{#if day === null}
					<div class="calendar-day empty"></div>
				{:else}
					<button
						type="button"
						class="calendar-day"
						class:selected={isSameDay(day, selectedDate)}
						class:today={isToday(day)}
						class:disabled={isDisabled(day)}
						disabled={isDisabled(day)}
						onclick={() => selectDate(day)}
					>
						{day.getDate()}
					</button>
				{/if}
			{/each}
		</div>

		<div class="calendar-footer">
			<button type="button" class="calendar-link" onclick={clearDate}>
				{getMessage('clear')}
			</button>
			<button type="button" class="calendar-link" onclick={selectToday}>
				{getMessage('today')}
			</button>
		</div>
	</div>
</div>

{#if error}
	<div id={id ? `${id}-error` : undefined} class="invalid-feedback">
		{#if Array.isArray(error)}
			{#each error as message}
				<div>{message}</div>
			{/each}
		{:else}
			{error}
		{/if}
	</div>
{/if}

<style>
	.date-picker-container {
		position: relative;
		width: 9em;
		max-width: 100%;
	}

	.date-picker-input {
		display: block;
		width: 9em;
		padding: 0.375rem 2.5rem 0.375rem 0.75rem;
		font-size: 1rem;
		font-weight: 400;
		line-height: 1.5;
		color: var(--ironpack-font-black);
		background-color: var(--ironpack-white);
		border: 1px solid var(--bs-border-color);
		border-radius: 0.25rem;
		transition: all 0.15s ease-in-out;
		appearance: none;
		text-align: left;
		cursor: pointer;
	}

	.date-picker-text {
		display: block;
		overflow: hidden;
		text-overflow: ellipsis;
		white-space: nowrap;
	}

	.date-picker-text.placeholder {
		background-color: transparent;
	}

	.calendar-icon {
		position: absolute;
		right: 0.75rem;
		top: 50%;
		transform: translateY(-50%);
		font-size: 1.1rem;
		color: var(--ironpack-font-black);
		pointer-events: none;
		transition: color 0.15s ease-in-out;
	}

	.date-picker-input:focus {
		border-color: var(--bs-secondary);
		outline: 0;
		box-shadow: 0 0 0 0.25rem rgba(var(--bs-secondary-rgb), 0.25);
	}

	.date-picker-input:disabled {
		background-color: var(--bs-secondary-bg);
		opacity: 1;
		cursor: not-allowed;
	}

	.date-picker-input.is-invalid {
		border-color: var(--bs-danger);
	}

	.date-picker-input.is-invalid:focus {
		border-color: var(--bs-danger);
		box-shadow: 0 0 0 0.25rem rgba(var(--bs-danger-rgb), 0.25);
	}

	/* Calendar Dropdown - matches Select menu */
	.calendar-dropdown {
		position: absolute;
		left: 0;
		right: 0;
		top: calc(100% + 0.25rem);
		background: var(--ironpack-white);
		border: 1px solid var(--bs-secondary);
		border-radius: 0.25rem;
		box-shadow: 0 4px 12px rgba(var(--bs-secondary-rgb), 0.2);
		z-index: 1050;
		padding: 0.75rem;
		opacity: 0;
		transform: translateY(-4px);
		visibility: hidden;
		pointer-events: none;
		transition: all 0.15s ease-in-out;
		min-width: 280px;
	}

	.calendar-dropdown.open {
		opacity: 1;
		transform: translateY(0);
		visibility: visible;
		pointer-events: auto;
	}

	/* Calendar Header */
	.calendar-header {
		display: flex;
		align-items: center;
		justify-content: space-between;
		margin-bottom: 0.75rem;
		padding-bottom: 0.5rem;
		border-bottom: 1px solid var(--bs-border-color);
	}

	.calendar-month-year {
		font-weight: 600;
		color: var(--ironpack-font-black);
		font-size: 0.95rem;
	}

	.calendar-nav {
		background: none;
		border: none;
		color: var(--ironpack-font-black);
		cursor: pointer;
		padding: 0.25rem 0.5rem;
		border-radius: 0.25rem;
		transition: all 0.15s ease-in-out;
	}

	.calendar-nav:hover {
		background: rgba(var(--bs-secondary-rgb), 0.1);
		color: var(--ironpack-font-black);
	}

	/* Weekdays */
	.calendar-weekdays {
		display: grid;
		grid-template-columns: repeat(7, 1fr);
		gap: 0.25rem;
		margin-bottom: 0.5rem;
	}

	.calendar-weekday {
		text-align: center;
		font-size: 0.75rem;
		font-weight: 600;
		color: var(--ironpack-font-dark);
		padding: 0.25rem;
	}

	/* Calendar Days Grid */
	.calendar-days {
		display: grid;
		grid-template-columns: repeat(7, 1fr);
		gap: 0.25rem;
	}

	.calendar-day {
		aspect-ratio: 1;
		border: none;
		background: none;
		border-radius: 0.25rem;
		font-size: 0.875rem;
		color: var(--ironpack-font-black);
		cursor: pointer;
		transition: all 0.15s ease-in-out;
		display: flex;
		align-items: center;
		justify-content: center;
	}

	.calendar-day.empty {
		cursor: default;
	}

	/* Hover state - matches NavRail hover */
	.calendar-day:hover:not(.empty):not(.disabled):not(.selected) {
		background: rgba(var(--bs-secondary-rgb), 0.06);
		color: var(--ironpack-font-black);
	}

	/* Selected state - matches NavRail active */
	.calendar-day.selected {
		background: rgba(var(--bs-secondary-rgb), 0.16);
		color: var(--bs-secondary);
		font-weight: 600;
	}

	.calendar-day.selected:hover:not(.empty):not(.disabled),
	.calendar-day.selected:focus-visible:not(.empty):not(.disabled) {
		background: rgba(var(--bs-secondary-rgb), 0.18);
		color: var(--bs-secondary);
	}

	/* Today indicator */
	.calendar-day.today:not(.selected) {
		border: 1px solid var(--bs-secondary);
	}

	.calendar-day.disabled {
		color: var(--bs-secondary-color);
		cursor: not-allowed;
		opacity: 0.5;
	}

	/* Calendar Footer */
	.calendar-footer {
		display: flex;
		justify-content: space-between;
		margin-top: 0.75rem;
		padding-top: 0.5rem;
		border-top: 1px solid var(--bs-border-color);
	}

	.calendar-link {
		background: none;
		border: none;
		color: var(--ironpack-font-black);
		cursor: pointer;
		font-size: 0.875rem;
		padding: 0.25rem 0.5rem;
		border-radius: 0.25rem;
		transition: all 0.15s ease-in-out;
	}

	.calendar-link:hover {
		color: var(--ironpack-font-black);
		background: rgba(var(--bs-secondary-rgb), 0.1);
	}

	/* Invalid feedback */
	.invalid-feedback {
		display: block;
		width: 100%;
		margin-top: 0.25rem;
		font-size: 0.875em;
		color: var(--bs-danger);
	}
</style>
