<script lang="ts">
	/**
	 * Skeleton Loader for content placeholders
	 *
	 * @example
	 * <SkeletonLoader type="text" />
	 * <SkeletonLoader type="circle" size="lg" />
	 * <SkeletonLoader type="rect" width="100%" height="200px" />
	 */

	type SkeletonType = 'text' | 'rect' | 'circle';
	type Size = 'sm' | 'md' | 'lg';

	interface SkeletonProps {
		type?: SkeletonType;
		size?: Size;
		width?: string;
		height?: string;
		class?: string;
	}

	let {
		type = 'text',
		size = 'md',
		width,
		height,
		class: className = ''
	}: SkeletonProps = $props();

	const typeClass = $derived(`skeleton-${type}`);
	const sizeClass = $derived(`skeleton-${size}`);
	const classes = $derived(['skeleton', typeClass, sizeClass, className].filter(Boolean).join(' '));

	const style = $derived(() => {
		const styles: string[] = [];
		if (width) styles.push(`width: ${width}`);
		if (height) styles.push(`height: ${height}`);
		return styles.join('; ');
	});
</script>

<div class={classes} style={style()} aria-busy="true" aria-live="polite"></div>

<style>
	.skeleton {
		background: linear-gradient(90deg, #f0f0f0 25%, #e0e0e0 50%, #f0f0f0 75%);
		background-size: 200% 100%;
		animation: shimmer 1.5s ease-in-out infinite;
		border-radius: 4px;
	}

	/* Type variants */
	.skeleton-text {
		height: 1em;
		margin-bottom: 0.5rem;
	}

	.skeleton-circle {
		border-radius: 50%;
	}

	/* Size variants */
	.skeleton-sm {
		height: 1rem;
	}

	.skeleton-md {
		height: 2rem;
	}

	.skeleton-lg {
		height: 3rem;
	}

	.skeleton-circle.skeleton-sm {
		width: 2rem;
		height: 2rem;
	}

	.skeleton-circle.skeleton-md {
		width: 3rem;
		height: 3rem;
	}

	.skeleton-circle.skeleton-lg {
		width: 4rem;
		height: 4rem;
	}

	@keyframes shimmer {
		0% {
			background-position: -200% 0;
		}
		100% {
			background-position: 200% 0;
		}
	}
</style>
