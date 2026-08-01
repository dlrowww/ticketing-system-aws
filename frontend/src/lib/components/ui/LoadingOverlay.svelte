<script lang="ts">
	import Spinner from './Spinner.svelte';
	import { getMessage } from '$lib/i18n';

	/**
	 * Loading Overlay for blocking UI during async operations
	 * 
	 * @example
	 * <LoadingOverlay show={isLoading} message="Saving ticket..." />
	 */

	interface LoadingOverlayProps {
		show?: boolean;
		message?: string;
		fullscreen?: boolean;
		class?: string;
	}

	let {
		show = false,
		message,
		fullscreen = false,
		class: className = ''
	}: LoadingOverlayProps = $props();

	const defaultMessage = $derived(getMessage('loading'));
	const displayMessage = $derived(message ?? defaultMessage);
	const classes = $derived(['loading-overlay', fullscreen ? 'fullscreen' : '', className].filter(Boolean).join(' '));
</script>

{#if show}
	<div class={classes}>
		<div class="loading-content">
			<Spinner size="lg" />
			<p class="loading-message">{displayMessage}</p>
		</div>
	</div>
{/if}

<style>
	.loading-overlay {
		position: absolute;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: rgba(255, 255, 255, 0.9);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1000;
	}

	.loading-overlay.fullscreen {
		position: fixed;
		z-index: 9998;
	}

	.loading-content {
		text-align: center;
	}

	.loading-message {
		margin-top: 1rem;
		font-size: 0.95rem;
		color: #555;
		font-weight: 500;
	}
</style>
