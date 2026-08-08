<script lang="ts">
	/**
	 * Generic Input component - reusable form primitive
	 *
	 * @example
	 * <Input type="text" bind:value={name} placeholder="Enter name" />
	 * <Input type="email" bind:value={email} error="Invalid email" />
	 */

	type InputType =
		| 'text'
		| 'email'
		| 'password'
		| 'number'
		| 'tel'
		| 'url'
		| 'search'
		| 'date'
		| 'time'
		| 'datetime-local'
		| 'month'
		| 'week';

	interface InputProps {
		type?: InputType;
		value?: string | number;
		placeholder?: string;
		disabled?: boolean;
		readonly?: boolean;
		required?: boolean;
		class?: string;
		error?: string | string[];
		id?: string;
		name?: string;
		autocomplete?: HTMLInputElement['autocomplete'];
		min?: string | number;
		max?: string | number;
		step?: string | number;
		pattern?: string;
		maxlength?: number;
		title?: string;
		ellipsis?: boolean;
	}

	let {
		type = 'text',
		value = $bindable(),
		placeholder,
		disabled = false,
		readonly = false,
		required = false,
		class: className = '',
		error,
		id,
		name,
		autocomplete,
		min,
		max,
		step,
		pattern,
		maxlength,
		title: titleAttr,
		ellipsis = false
	}: InputProps = $props();

	const inputClass = $derived(
		['form-control', error ? 'is-invalid' : '', ellipsis ? 'input-ellipsis' : '', className]
			.filter(Boolean)
			.join(' ')
	);

	const computedTitle = $derived(() => {
		if (titleAttr !== undefined) return titleAttr;
		if (!ellipsis) return undefined;
		const rawValue = value === null || value === undefined ? '' : String(value);
		if (rawValue.trim().length > 0) {
			return rawValue;
		}
		const placeholderText = placeholder ?? '';
		return placeholderText.trim().length > 0 ? placeholderText : undefined;
	});
</script>

<input
	{type}
	bind:value
	{placeholder}
	{disabled}
	{readonly}
	{required}
	{id}
	{name}
	{autocomplete}
	{min}
	{max}
	{step}
	{pattern}
	{maxlength}
	class={inputClass}
	title={computedTitle()}
	aria-invalid={error ? 'true' : undefined}
	aria-describedby={error && id ? `${id}-error` : undefined}
/>

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
	.form-control {
		display: block;
		width: 100%;
		padding: 0.375rem 0.75rem;
		font-size: 1rem;
		font-weight: 400;
		line-height: 1.5;
		color: var(--ironpack-font-black);
		background-color: #fff;
		background-clip: padding-box;
		border: 1px solid var(--bs-border-color);
		border-radius: 0.25rem;
		transition:
			border-color 0.15s ease-in-out,
			box-shadow 0.15s ease-in-out;
	}

	.form-control:focus {
		border-color: var(--ironpack-border);
		outline: 0;
		box-shadow: 0 0 0 0.25rem rgba(var(--ironpack-border-rgb), 0.2);
	}

	.form-control.input-ellipsis {
		overflow: hidden;
	}

	.form-control.input-ellipsis:not(:focus) {
		white-space: nowrap;
		text-overflow: ellipsis;
	}

	/* Style the search clear button (x) to be black like Select's chevron */
	.form-control[type='search']::-webkit-search-cancel-button {
		-webkit-appearance: none;
		appearance: none;
		height: 0.8em;
		width: 0.8em;
		background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 16 16' fill='%23000'%3e%3cpath d='M2.146 2.854a.5.5 0 1 1 .708-.708L8 7.293l5.146-5.147a.5.5 0 0 1 .708.708L8.707 8l5.147 5.146a.5.5 0 0 1-.708.708L8 8.707l-5.146 5.147a.5.5 0 0 1-.708-.708L7.293 8 2.146 2.854Z'/%3e%3c/svg%3e");
		background-repeat: no-repeat;
		background-position: center;
		background-color: rgba(var(--ironpack-font-black-rgb), 0.2);
		cursor: pointer;
	}

	.form-control:disabled,
	.form-control[readonly] {
		background-color: #e9ecef;
		opacity: 1;
	}

	.form-control.is-invalid {
		border-color: #dc3545;
		padding-right: calc(1.5em + 0.75rem);
		background-image: url("data:image/svg+xml,%3csvg xmlns='http://www.w3.org/2000/svg' viewBox='0 0 12 12' width='12' height='12' fill='none' stroke='%23dc3545'%3e%3ccircle cx='6' cy='6' r='4.5'/%3e%3cpath stroke-linejoin='round' d='M5.8 3.6h.4L6 6.5z'/%3e%3ccircle cx='6' cy='8.2' r='.6' fill='%23dc3545' stroke='none'/%3e%3c/svg%3e");
		background-repeat: no-repeat;
		background-position: right calc(0.375em + 0.1875rem) center;
		background-size: calc(0.75em + 0.375rem) calc(0.75em + 0.375rem);
	}

	.form-control.is-invalid:focus {
		border-color: #dc3545;
		box-shadow: 0 0 0 0.25rem rgba(220, 53, 69, 0.25);
	}

	.invalid-feedback {
		display: block;
		width: 100%;
		margin-top: 0.25rem;
		font-size: 0.875em;
		color: #dc3545;
	}
</style>
