<script lang="ts">
	import { onMount } from 'svelte';
	import { fade, fly } from 'svelte/transition';
	import type { Toast } from '$lib/stores/toast';
	import { toastStore } from '$lib/stores/toast';
	import { getMessage } from '$lib/i18n';

	let { toast }: { toast: Toast } = $props();

	// Bootstrap icon mapping for toast types
	const icons: Record<Toast['type'], string> = {
		success: 'bi-check-circle-fill',
		error: 'bi-x-circle-fill',
		warning: 'bi-exclamation-triangle-fill',
		info: 'bi-info-circle-fill'
	};

	// Bootstrap alert class mapping
	const alertClasses: Record<Toast['type'], string> = {
		success: 'alert-success',
		error: 'alert-danger',
		warning: 'alert-warning',
		info: 'alert-info'
	};

	function handleClose() {
		toastStore.remove(toast.id);
	}
</script>

<div
	class="toast-item alert {alertClasses[toast.type]} d-flex align-items-center"
	role="alert"
	transition:fly={{ y: -20, duration: 300 }}
>
	<i class="toast-icon me-2 bi {icons[toast.type]}"></i>
	<span class="toast-message flex-grow-1">{toast.message}</span>
	<button type="button" class="btn-close" aria-label={getMessage('close')} onclick={handleClose}
	></button>
</div>

<style>
	.toast-item {
		margin-bottom: 0.5rem;
		min-width: 300px;
		max-width: 500px;
		box-shadow: 0 4px 6px rgba(0, 0, 0, 0.1);
		border-left: 4px solid;
	}

	.toast-icon {
		font-size: 1.25rem;
		font-weight: bold;
	}

	.toast-message {
		font-size: 0.9rem;
		word-break: break-word;
	}

	/* Override Bootstrap alert padding for better toast appearance */
	.toast-item {
		padding: 0.75rem 1rem;
	}

	/* Border colors for each type */
	.toast-item.alert-success {
		border-left-color: #198754;
	}

	.toast-item.alert-danger {
		border-left-color: #dc3545;
	}

	.toast-item.alert-warning {
		border-left-color: #ffc107;
	}

	.toast-item.alert-info {
		border-left-color: #0dcaf0;
	}
</style>
