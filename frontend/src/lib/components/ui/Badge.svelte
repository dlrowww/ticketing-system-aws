<script lang="ts">
	/**
	 * Generic Badge component - reusable UI primitive
	 * 
	 * @example
	 * <Badge variant="primary">New</Badge>
	 * <Badge variant="success" size="sm" label="Resolved" />
	 * <Badge variant="custom" class="bg-status-open">Open</Badge>
	 */

	type Variant = 'primary' | 'secondary' | 'success' | 'danger' | 'warning' | 'info' | 'light' | 'dark' | 'custom';
	type Size = 'sm' | 'md' | 'lg';

	interface BadgeProps {
		variant?: Variant;
		size?: Size;
		class?: string;
		ariaLabel?: string;
		label?: string; // Simple string label (alternative to children snippet)
		children?: import('svelte').Snippet;
	}

	let {
		variant = 'primary',
		size = 'md',
		class: className = '',
		ariaLabel,
		label,
		children
	}: BadgeProps = $props();

	const variantClass = $derived(variant === 'custom' ? '' : `bg-${variant}`);
	const sizeClass = $derived({
		sm: 'badge-sm',
		md: 'badge-md',
		lg: 'badge-lg'
	}[size]);

	const classes = $derived(['badge', variantClass, sizeClass, className].filter(Boolean).join(' '));
</script>

<span class={classes} aria-label={ariaLabel}>
	{#if label}
		{label}
	{:else if children}
		{@render children()}
	{/if}
</span>

<style>
	.badge {
		display: inline-block;
		padding: 0.35rem 0.7rem;
		border-radius: 999px;
		line-height: 1;
		white-space: nowrap;
		font-weight: 500;
		text-align: center;
	}

	/* Size variants */
	.badge-sm {
		font-size: 0.75rem;
		padding: 0.25rem 0.5rem;
	}

	.badge-md {
		font-size: 0.85rem;
		padding: 0.35rem 0.7rem;
	}

	.badge-lg {
		font-size: 1rem;
		padding: 0.45rem 0.9rem;
	}

	/* Bootstrap-style background variants (colors from Bootstrap 5) */
	.bg-primary {
		background-color: var(--ironpack-red);
		color: var(--ironpack-white);
	}

	.bg-secondary {
		background-color: #dee2e6 !important;
		color: #495057;
	}

	.bg-success {
		background-color: #198754;
		color: white;
	}

	.bg-danger {
		background-color: var(--ironpack-red);
		color: var(--ironpack-white);
	}

	.bg-warning {
		background-color: #ffc107;
		color: #000;
	}

	.bg-info {
		background-color: #0dcaf0;
		color: #000;
	}

	.bg-light {
		background-color: #f8f9fa;
		color: #212529;
	}

	.bg-dark {
		background-color: #212529;
		color: white;
	}
</style>
