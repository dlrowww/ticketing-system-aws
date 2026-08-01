<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import type { AssignableUserDto } from '$lib/types/users';
	import Select from './Select.svelte';

	let {
		ticketId,
		value = $bindable(),
		users,
		excludeUserIds = [],
		id,
		name,
		loading = false,
		disabled = false,
		required = false
	}: {
		ticketId: number;
		value?: number | null;
		users: AssignableUserDto[];
		excludeUserIds?: Array<string | number>;
		id?: string;
		name?: string;
		loading?: boolean;
		disabled?: boolean;
		required?: boolean;
	} = $props();

	// Internal state for Select component (uses string | number | '')
	let selectValue = $state<string | number | ''>(value ?? '');

	// Exclude the currently selected value from the dropdown.
	// Do NOT rely on finding the user in `users` (list may be merged/partial).
	const excludedValues = $derived.by<(string | number)[]>(() => {
		const currentUserId = value ?? selectValue;
		if (currentUserId === '' || currentUserId === null || currentUserId === undefined) return [];
		return [currentUserId];
	});

	const excludeFromDropdown = $derived.by<(string | number)[]>(() => {
		const combined = [...(excludeUserIds ?? []), ...excludedValues];
		const seen = new Set<string>();
		const result: Array<string | number> = [];
		for (const v of combined) {
			if (v === '' || v === null || v === undefined) continue;
			const key = String(v);
			if (seen.has(key)) continue;
			seen.add(key);
			result.push(v);
		}
		return result;
	});

	// Transform users into Select options format
	// Include current user for display, but we'll filter in rendering
	const options = $derived.by(() => {
		if (loading || users.length === 0) {
			// Dropdown is disabled in these states; options won't be shown.
			return [];
		}

		const userOptions = users.map(user => ({
			value: user.userId,
			label: `${user.name}${user.roleName ? ` (${user.roleName})` : ''}`
		}));

		// Add "Unassigned" option if not required
		if (!required) {
			return [
				{ value: '', label: `— ${getMessage('not_assigned')} —` },
				...userOptions
			];
		}

		return userOptions;
	});

	const placeholderText = $derived.by(() => {
		if (loading) return getMessage('loading');
		if (!loading && users.length === 0) return getMessage('no_assignable_users');
		return getMessage('assigned_to');
	});

	// Sync parent value changes to selectValue
	$effect(() => {
		selectValue = value ?? '';
	});

	// Sync selectValue changes to parent value
	$effect(() => {
		if (selectValue === '') {
			value = null;
		} else if (typeof selectValue === 'number') {
			value = selectValue;
		} else if (typeof selectValue === 'string' && selectValue !== '') {
			value = parseInt(selectValue, 10);
		}
	});
</script>

<Select
	bind:value={selectValue}
	options={options}
	excludeFromDropdown={excludeFromDropdown}
	disabled={disabled || loading || users.length === 0}
	placeholder={placeholderText}
	id={id}
	name={name}
/>
