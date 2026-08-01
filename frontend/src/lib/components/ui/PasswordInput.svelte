<script lang="ts">
	import { getMessage } from '$lib/i18n';
	
	/**
	 * Password Input component with visibility toggle
	 * 
	 * @example
	 * <PasswordInput bind:value={password} placeholder="Enter password" />
	 * <PasswordInput bind:value={password} error="Password required" />
	 */

	interface PasswordInputProps {
		value?: string;
		placeholder?: string;
		disabled?: boolean;
		readonly?: boolean;
		required?: boolean;
		class?: string;
		error?: string | string[];
		id?: string;
		name?: string;
		autocomplete?: HTMLInputElement['autocomplete'];
		maxlength?: number;
	}

	let {
		value = $bindable(''),
		placeholder,
		disabled = false,
		readonly = false,
		required = false,
		class: className = '',
		error,
		id,
		name,
		autocomplete,
		maxlength
	}: PasswordInputProps = $props();

	let showPassword = $state(false);

	const inputClass = $derived(
		['form-control', error ? 'is-invalid' : '', className]
			.filter(Boolean)
			.join(' ')
	);

	function toggleVisibility(e: MouseEvent) {
		e.preventDefault();
		e.stopPropagation();
		showPassword = !showPassword;
	}
	
	const toggleLabel = $derived(showPassword ? getMessage('hide_password') : getMessage('show_password'));
</script>

<div class="password-input-wrapper">
	<input
		type={showPassword ? 'text' : 'password'}
		bind:value
		{placeholder}
		{disabled}
		{readonly}
		{required}
		{id}
		{name}
		{autocomplete}
		{maxlength}
		class={inputClass}
		aria-invalid={error ? 'true' : undefined}
		aria-describedby={error && id ? `${id}-error` : undefined}
	/>
	<button
		type="button"
		class="password-toggle-btn"
		onclick={toggleVisibility}
		disabled={disabled}
		aria-label={toggleLabel}
		tabindex="-1"
	>
		<i class="bi {showPassword ? 'bi-eye-slash' : 'bi-eye'}"></i>
	</button>
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
	.password-input-wrapper {
		position: relative;
		display: block;
		width: 100%;
	}

	.password-input-wrapper input {
		width: 100%;
		padding-right: 2.75rem;
	}

	.password-toggle-btn {
		position: absolute;
		right: 0.5rem;
		top: 50%;
		transform: translateY(-50%);
		background: transparent;
		border: none;
		padding: 0.25rem 0.5rem;
		cursor: pointer;
		display: flex;
		align-items: center;
		justify-content: center;
		color: #6c757d;
		transition: color 0.15s ease-in-out;
		z-index: 10;
	}

	.password-toggle-btn:hover:not(:disabled) {
		color: #495057;
	}

	.password-toggle-btn:disabled {
		cursor: not-allowed;
		opacity: 0.5;
	}

	.password-toggle-btn:focus {
		outline: none;
		color: #495057;
	}

	.password-toggle-btn i {
		font-size: 1.1rem;
		pointer-events: none;
	}
</style>
