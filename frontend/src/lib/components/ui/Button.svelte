<script lang="ts">
	import Spinner from './Spinner.svelte';

	/**
	 * Generic Button component - reusable UI primitive
	 * 
	 * @example
	 * <Button variant="primary" onclick={() => alert('clicked')}>Submit</Button>
	 * <Button variant="secondary" size="sm" disabled>Cancel</Button>
	 * <Button variant="primary" loading>Saving...</Button>
	 */

	type Variant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark' | 'link' | 'outline-primary' | 'outline-secondary';
	type Size = 'sm' | 'md' | 'lg';
	type ButtonType = 'button' | 'submit' | 'reset';

	interface ButtonProps {
		variant?: Variant;
		size?: Size;
		type?: ButtonType;
		disabled?: boolean;
		loading?: boolean; // Show spinner and disable button
		class?: string;
		onclick?: (e: MouseEvent) => void;
		label?: string; // Simple string label
		children?: import('svelte').Snippet;
	}

	let {
		variant = 'primary',
		size = 'md',
		type = 'button',
		disabled = false,
		loading = false,
		class: className = '',
		onclick,
		label,
		children
	}: ButtonProps = $props();

	const variantClass = $derived(`btn-${variant}`);
	const sizeClass = $derived(size === 'md' ? '' : `btn-${size}`);
	const classes = $derived(['btn', variantClass, sizeClass, className].filter(Boolean).join(' '));
	const isDisabled = $derived(disabled || loading);

	// Spinner size based on button size
	const spinnerSize = $derived(size === 'sm' ? 'sm' : size === 'lg' ? 'md' : 'sm');
	
	// Spinner variant based on button variant
	const spinnerVariant = $derived(() => {
		if (variant.includes('outline') || variant === 'light' || variant === 'link') {
			return 'primary';
		}
		return 'light';
	});
</script>

<button
	{type}
	disabled={isDisabled}
	class={classes}
	onclick={onclick}
>
	{#if loading}
		<span class="btn-spinner">
			<Spinner size={spinnerSize} variant={spinnerVariant()} />
		</span>
	{/if}
	<span class:btn-content-loading={loading}>
		{#if label}
			{label}
		{:else if children}
			{@render children()}
		{/if}
	</span>
</button>

<style>
	.btn {
		display: inline-block;
		padding: 0.375rem 0.75rem;
		font-size: 1rem;
		line-height: 1.5;
		border-radius: 0.25rem;
		border: 1px solid transparent;
		cursor: pointer;
		text-align: center;
		text-decoration: none;
		vertical-align: middle;
		user-select: none;
		transition: color 0.15s ease-in-out, background-color 0.15s ease-in-out,
			border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
	}

	.btn:disabled {
		opacity: 0.65;
		cursor: not-allowed;
	}

	/* Size variants */
	.btn-sm {
		padding: 0.25rem 0.5rem;
		font-size: 0.875rem;
	}

	.btn-lg {
		padding: 0.5rem 1rem;
		font-size: 1.25rem;
	}

	/* Bootstrap-style button variants */
	.btn-primary {
		background-color: var(--ironpack-red);
		border-color: var(--ironpack-red);
		color: var(--ironpack-white);
	}

	.btn-primary:hover:not(:disabled) {
		background-color: var(--ironpack-font-black);
		border-color: var(--ironpack-font-black);
	}

	.btn-primary:focus-visible:not(:disabled) {
		background-color: var(--ironpack-red);
		border-color: var(--ironpack-red);
		box-shadow: 0 0 0 0.2rem rgba(var(--ironpack-red-rgb), 0.4);
		outline: none;
	}

	.btn-primary:active:not(:disabled) {
		background-color: var(--ironpack-red-dark);
		border-color: var(--ironpack-red-dark);
		box-shadow: inset 0 3px 5px rgba(0, 0, 0, 0.125);
	}

	.btn-secondary {
		background-color: #6c757d;
		border-color: #6c757d;
		color: white;
	}

	.btn-secondary:hover:not(:disabled) {
		background-color: #5c636a;
		border-color: #565e64;
	}

	.btn-secondary:focus-visible:not(:disabled) {
		background-color: #6c757d;
		border-color: #6c757d;
		box-shadow: 0 0 0 0.2rem rgba(108, 117, 125, 0.5);
		outline: none;
	}

	.btn-secondary:active:not(:disabled) {
		background-color: #565e64;
		border-color: #51585e;
		box-shadow: inset 0 3px 5px rgba(0, 0, 0, 0.125);
	}

	.btn-success {
		background-color: #198754;
		border-color: #198754;
		color: white;
	}

	.btn-success:hover:not(:disabled) {
		background-color: #157347;
		border-color: #146c43;
	}

	.btn-danger {
		background-color: var(--ironpack-red);
		border-color: var(--ironpack-red);
		color: var(--ironpack-white);
	}

	.btn-danger:hover:not(:disabled) {
		background-color: var(--ironpack-font-black);
		border-color: var(--ironpack-font-black);
	}

	.btn-danger:focus-visible:not(:disabled) {
		background-color: var(--ironpack-red);
		border-color: var(--ironpack-red);
		box-shadow: 0 0 0 0.2rem rgba(var(--ironpack-red-rgb), 0.4);
		outline: none;
	}

	.btn-danger:active:not(:disabled) {
		background-color: var(--ironpack-red-dark);
		border-color: var(--ironpack-red-dark);
		box-shadow: inset 0 3px 5px rgba(0, 0, 0, 0.125);
	}

	.btn-warning {
		background-color: #ffc107;
		border-color: #ffc107;
		color: #000;
	}

	.btn-warning:hover:not(:disabled) {
		background-color: #ffca2c;
		border-color: #ffc720;
	}

	.btn-info {
		background-color: #0dcaf0;
		border-color: #0dcaf0;
		color: #000;
	}

	.btn-info:hover:not(:disabled) {
		background-color: #31d2f2;
		border-color: #25cff2;
	}

	.btn-light {
		background-color: #f8f9fa;
		border-color: #f8f9fa;
		color: #000;
	}

	.btn-light:hover:not(:disabled) {
		background-color: #f9fafb;
		border-color: #f9fafb;
	}

	.btn-dark {
		background-color: #212529;
		border-color: #212529;
		color: white;
	}

	.btn-dark:hover:not(:disabled) {
		background-color: #1c1f23;
		border-color: #1a1e21;
	}

	.btn-link {
		background-color: transparent;
		border-color: transparent;
		color: var(--ironpack-font-black);
		text-decoration: underline;
	}

	.btn-link:hover:not(:disabled) {
		color: var(--ironpack-red);
	}

	/* Outline variants */
	.btn-outline-primary {
		background-color: transparent;
		border-color: var(--ironpack-red);
		color: var(--ironpack-red);
	}

	.btn-outline-primary:hover:not(:disabled) {
		background-color: var(--ironpack-font-black);
		border-color: var(--ironpack-font-black);
		color: var(--ironpack-white);
	}

	.btn-outline-primary:focus-visible:not(:disabled) {
		background-color: transparent;
		border-color: var(--ironpack-red);
		color: var(--ironpack-red);
		box-shadow: 0 0 0 0.2rem rgba(var(--ironpack-red-rgb), 0.4);
		outline: none;
	}

	.btn-outline-primary:active:not(:disabled) {
		background-color: var(--ironpack-red);
		border-color: var(--ironpack-red);
		color: var(--ironpack-white);
		box-shadow: inset 0 3px 5px rgba(0, 0, 0, 0.125);
	}

	.btn-outline-secondary {
		background-color: transparent;
		border-color: #6c757d;
		color: #6c757d;
	}

	.btn-outline-secondary:hover:not(:disabled) {
		background-color: #6c757d;
		border-color: #6c757d;
		color: white;
	}

	/* Loading state */
	.btn-spinner {
		display: inline-block;
		margin-right: 0.5rem;
		vertical-align: middle;
	}

	.btn-content-loading {
		opacity: 0.7;
	}
</style>
