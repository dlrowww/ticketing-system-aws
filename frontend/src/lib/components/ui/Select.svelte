<script lang="ts">
	/**
	 * Generic Select component - reusable form primitive
	 * 
	 * @example
	 * <Select bind:value={category} options={categoryOptions} placeholder="Select category" />
	 * <Select bind:value={priority} options={priorityOptions} error="Priority is required" />
	 */
	import { getMessage } from '$lib/i18n';
	import { onMount, tick } from 'svelte';

	// Module-scoped width groups so multiple Selects can share a computed width.
	type WidthGroup = { width: number; listeners: Set<(w: number) => void> };
	const WIDTH_GROUPS = new Map<string, WidthGroup>();

	function subscribeWidthGroup(key: string, listener: (w: number) => void): () => void {
		const existing = WIDTH_GROUPS.get(key);
		const group: WidthGroup = existing ?? { width: 0, listeners: new Set() };
		group.listeners.add(listener);
		WIDTH_GROUPS.set(key, group);
		listener(group.width);
		return () => group.listeners.delete(listener);
	}

	function publishWidthGroup(key: string, nextWidth: number) {
		const existing = WIDTH_GROUPS.get(key);
		const group: WidthGroup = existing ?? { width: 0, listeners: new Set() };
		if (nextWidth <= group.width) {
			WIDTH_GROUPS.set(key, group);
			return;
		}
		group.width = nextWidth;
		WIDTH_GROUPS.set(key, group);
		group.listeners.forEach((l) => l(group.width));
	}

	interface Option {
		value: string | number;
		labelKey?: string; // i18n key (for compatibility with existing Lookups)
		label?: string; // Optional literal label (already user-facing)
	}

	interface SelectProps {
		value?: string | number | '' | undefined;
		options?: Option[];
		placeholder?: string;
		disabled?: boolean;
		required?: boolean;
		/**
		 * When set, Selects sharing the same key will match the widest Select in the group.
		 * Useful for filter toolbars where multiple Selects should have the same width.
		 */
		widthGroup?: string;
		/**
		 * Values to exclude from the dropdown menu (but still show as selected in the button).
		 * Useful for showing current selection without allowing it to be re-selected.
		 */
		excludeFromDropdown?: (string | number)[];
		class?: string;
		style?: string;
		error?: string | string[];
		id?: string;
		name?: string;
		icon?: string;
	}

	let {
		value = $bindable<string | number | '' | undefined>(undefined),
		options = [],
		placeholder,
		disabled = false,
		required = false,
		widthGroup,
		excludeFromDropdown = [],
		class: className = '',
		style: styleAttr,
		error,
		id,
		name,
		icon
	}: SelectProps = $props();

	let open = $state(false);
	let buttonEl = $state<HTMLButtonElement | null>(null);
	let menuPosition = $state<{ top: number; left: number; width: number } | null>(null);

	const selectedOption = $derived.by(() => {
		if (value === '' || value === null || value === undefined) {
			return undefined;
		}
		return options.find((opt) => String(opt.value) === String(value));
	});

	const displayText = $derived.by(() => {
		if (!selectedOption) {
			return placeholder ?? '';
		}
		return selectedOption.label ?? getMessage(selectedOption.labelKey ?? '');
	});

	const excludeFromDropdownSet = $derived.by(() => {
		// Normalize to strings so `2` and "2" are treated the same.
		return new Set(excludeFromDropdown.map((v) => String(v)));
	});

	function toggleOpen() {
		if (disabled) {
			return;
		}
		open = !open;
		if (open && buttonEl) {
			updateMenuPosition();
		}
	}

	function updateMenuPosition() {
		if (!buttonEl) return;
		const rect = buttonEl.getBoundingClientRect();
		menuPosition = {
			top: rect.bottom + 4, // 4px gap
			left: rect.left,
			width: rect.width
		};
	}

	function close() {
		open = false;
	}

	function selectValue(next: string | number) {
		value = next;
		open = false;
	}

	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('.select-container')) {
			open = false;
		}
	}

	function handleKeydown(event: KeyboardEvent) {
		if (disabled) {
			return;
		}

		if (event.key === 'Escape') {
			open = false;
			return;
		}

		if (event.key === 'Enter' || event.key === ' ') {
			event.preventDefault();
			open = !open;
		}

		if (event.key === 'ArrowDown') {
			event.preventDefault();
			open = true;
		}
	}

	$effect(() => {
		if (open) {
			document.addEventListener('click', handleClickOutside);
			window.addEventListener('scroll', updateMenuPosition, true);
			window.addEventListener('resize', updateMenuPosition);
			return () => {
				document.removeEventListener('click', handleClickOutside);
				window.removeEventListener('scroll', updateMenuPosition, true);
				window.removeEventListener('resize', updateMenuPosition);
			};
		}
	});

	const selectClass = $derived(['form-select', error ? 'is-invalid' : '', className].filter(Boolean).join(' '));
	
	const showPlaceholder = $derived(
		Boolean(placeholder && (value === '' || value === null || value === undefined))
	);

	let measureEl = $state<HTMLButtonElement | null>(null);
	let ownMinWidth = $state<number>(0);
	let groupMinWidth = $state<number>(0);
	let unsubscribeGroup = $state<null | (() => void)>(null);

	const appliedMinWidth = $derived(Math.max(ownMinWidth, groupMinWidth));
	const containerStyle = $derived.by(() => {
		const parts: string[] = [];
		if (styleAttr) parts.push(styleAttr);
		if (appliedMinWidth > 0) parts.push(`--select-min-width: ${appliedMinWidth}px`);
		return parts.join('; ');
	});

	function optionLabel(opt: Option): string {
		return opt.label ?? getMessage(opt.labelKey ?? '');
	}

	async function computeMinWidth() {
		if (!measureEl) return;
		await tick();
		// Component may unmount between scheduling and tick().
		if (!measureEl) return;
		const el = measureEl;
		const texts: string[] = [];
		if (placeholder) texts.push(placeholder);
		for (const opt of options) {
			texts.push(optionLabel(opt));
		}

		let max = 0;
		for (const text of texts) {
			el.textContent = text;
			max = Math.max(max, Math.ceil(el.getBoundingClientRect().width));
		}
		ownMinWidth = max;
		if (widthGroup && max > 0) {
			publishWidthGroup(widthGroup, max);
		}
	}

	onMount(() => {
		if (widthGroup) {
			unsubscribeGroup = subscribeWidthGroup(widthGroup, (w) => (groupMinWidth = w));
		}
		void computeMinWidth();
		return () => unsubscribeGroup?.();
	});

	$effect(() => {
		// Recompute when opened (ensures correct sizing after locale changes).
		if (open) {
			void computeMinWidth();
		}
	});
</script>

<div class="select-container" style={containerStyle}>
	<button
		bind:this={buttonEl}
		type="button"
		id={id}
		class={selectClass}
		disabled={disabled}
		aria-describedby={error && id ? `${id}-error` : undefined}
		aria-haspopup="listbox"
		aria-expanded={open}
		onclick={toggleOpen}
		onkeydown={handleKeydown}
	>
		{#if icon}
			<i class={icon}></i>
		{/if}
		<span class="select-text" class:placeholder={showPlaceholder}>{displayText}</span>
	</button>

	<!-- Hidden measurement element used to compute min width based on the widest option. -->
	<button bind:this={measureEl} type="button" class="form-select select-measure" tabindex="-1" aria-hidden="true"></button>

	{#if name}
		<input type="hidden" name={name} value={value ?? ''} />
	{/if}
</div>

<!-- Dropdown menu rendered with fixed positioning to appear over modals -->
{#if open && menuPosition}
	<div 
		class="select-menu" 
		class:open 
		role="listbox" 
		aria-hidden={!open}
		style="top: {menuPosition.top}px; left: {menuPosition.left}px; width: {menuPosition.width}px;"
	>
		{#if placeholder}
			<button
				type="button"
				class="select-option"
				class:selected={showPlaceholder}
				role="option"
				aria-selected={showPlaceholder}
				onclick={() => selectValue('')}
			>
				{placeholder}
			</button>
		{/if}
		{#each options.filter((opt) => !excludeFromDropdownSet.has(String(opt.value))) as option (String(option.value))}
			<button
				type="button"
				class="select-option"
				class:selected={String(option.value) === String(value)}
				role="option"
				aria-selected={String(option.value) === String(value)}
				onclick={() => selectValue(option.value)}
			>
				{option.label ?? getMessage(option.labelKey ?? '')}
			</button>
		{/each}
	</div>
{/if}

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
	.select-container {
		position: relative;
		width: 100%;
		max-width: 100%;
		min-width: var(--select-min-width, 0px);
	}

	.form-select {
		display: block;
		width: 100%;
		padding: 0.375rem 2.25rem 0.375rem 0.75rem;
		font-size: 1rem;
		font-weight: 400;
		line-height: 1.5;
		color: var(--ironpack-font-black);
		background-color: var(--ironpack-white);
		background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16'%3e%3cpath fill='none' stroke='%23343a40' stroke-linecap='round' stroke-linejoin='round' stroke-width='2' d='M2 5l6 6 6-6'/%3e%3c/svg%3e");
		background-repeat: no-repeat;
		background-position: right 0.75rem center;
		background-size: 16px 12px;
		border: 1px solid var(--bs-border-color);
		border-radius: 0.25rem;
		transition: all 0.15s ease-in-out;
		appearance: none;
		text-align: left;
		cursor: pointer;
	}

	.form-select:focus {
		border-color: var(--bs-secondary);
		outline: 0;
		box-shadow: 0 0 0 0.25rem rgba(var(--bs-secondary-rgb), 0.25);
	}

	.form-select:disabled {
		background-color: var(--bs-secondary-bg);
		opacity: 1;
		cursor: not-allowed;
	}

	.form-select.is-invalid {
		border-color: var(--bs-danger);
	}

	.form-select.is-invalid:focus {
		border-color: var(--bs-danger);
		box-shadow: 0 0 0 0.25rem rgba(var(--bs-danger-rgb), 0.25);
	}

	.select-text.placeholder {
		background-color: transparent;
	}

	.select-menu {
		position: fixed;
		background: var(--ironpack-white);
		border: 1px solid var(--bs-border-color);
		border-radius: 0.25rem;
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
		z-index: 1060;
		max-height: min(50vh, 420px);
		overflow: auto;
		opacity: 0;
		transform: translateY(-4px);
		visibility: hidden;
		pointer-events: none;
		transition: all 0.15s ease-in-out;
	}

	.select-menu.open {
		opacity: 1;
		transform: translateY(0);
		visibility: visible;
		pointer-events: auto;
	}

	.select-option {
		display: block;
		width: 100%;
		text-align: left;
		padding: 0.5rem 0.75rem;
		border: none;
		background: none;
		color: var(--ironpack-font-black);
		cursor: pointer;
		transition: all 0.15s ease-in-out;
		white-space: nowrap;
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.select-measure {
		position: absolute;
		visibility: hidden;
		pointer-events: none;
		height: 0;
		padding-top: 0;
		padding-bottom: 0;
		border: 0;
		width: auto !important;
		white-space: nowrap;
		left: -99999px;
		top: -99999px;
	}

	.select-option:hover,
	.select-option:focus {
		background: rgba(var(--bs-secondary-rgb), 0.06);
		color: var(--ironpack-font-black);
		outline: none;
	}

	.select-option.selected {
		background: rgba(var(--bs-secondary-rgb), 0.16);
		color: var(--bs-secondary);
		font-weight: 600;
	}

	.select-option.selected:hover,
	.select-option.selected:focus {
		background: rgba(var(--bs-secondary-rgb), 0.18);
		color: var(--bs-secondary);
	}

	.invalid-feedback {
		display: block;
		width: 100%;
		margin-top: 0.25rem;
		font-size: 0.875em;
		color: var(--bs-danger);
	}
</style>
