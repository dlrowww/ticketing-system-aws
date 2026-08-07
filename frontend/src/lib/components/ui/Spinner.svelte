<script lang="ts">
	/**
	 * Loading Spinner component
	 *
	 * @example
	 * <Spinner size="sm" />
	 * <Spinner size="lg" variant="primary" />
	 */

	type Size = 'sm' | 'md' | 'lg';
	type Variant = 'primary' | 'secondary' | 'light' | 'dark';

	interface SpinnerProps {
		size?: Size;
		variant?: Variant;
		class?: string;
		ariaLabel?: string;
	}

	let {
		size = 'md',
		variant = 'primary',
		class: className = '',
		ariaLabel = 'Loading...'
	}: SpinnerProps = $props();

	const sizeClass = $derived(`spinner-${size}`);
	const variantClass = $derived(`spinner-${variant}`);
	const classes = $derived(
		['spinner', sizeClass, variantClass, className].filter(Boolean).join(' ')
	);
</script>

<div class={classes} role="status" aria-label={ariaLabel}>
	<span class="visually-hidden">{ariaLabel}</span>
</div>

<style>
	.spinner {
		display: inline-block;
		border-radius: 50%;
		animation: spin 1s linear infinite;
		border-style: solid;
	}

	/* Size variants */
	.spinner-sm {
		width: 1rem;
		height: 1rem;
		border-width: 2px;
	}

	.spinner-md {
		width: 2rem;
		height: 2rem;
		border-width: 3px;
	}

	.spinner-lg {
		width: 3rem;
		height: 3rem;
		border-width: 4px;
	}

	/* Color variants */
	.spinner-primary {
		border-color: var(--ironpack-red);
		border-top-color: transparent;
	}

	.spinner-secondary {
		border-color: #6c757d;
		border-top-color: transparent;
	}

	.spinner-light {
		border-color: rgba(255, 255, 255, 0.3);
		border-top-color: white;
	}

	.spinner-dark {
		border-color: rgba(0, 0, 0, 0.1);
		border-top-color: #000;
	}

	@keyframes spin {
		to {
			transform: rotate(360deg);
		}
	}

	.visually-hidden {
		position: absolute;
		width: 1px;
		height: 1px;
		padding: 0;
		margin: -1px;
		overflow: hidden;
		clip: rect(0, 0, 0, 0);
		white-space: nowrap;
		border: 0;
	}
</style>
