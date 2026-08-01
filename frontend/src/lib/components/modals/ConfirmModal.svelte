<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import Button from '$lib/components/ui/Button.svelte';
	import { modalStore } from '$lib/stores/modal';

	let {
		title,
		message,
		confirmText,
		cancelText,
		variant = 'danger',
		onConfirm
	}: {
		title?: string;
		message: string;
		confirmText?: string;
		cancelText?: string;
		variant?: 'primary' | 'danger' | 'warning';
		onConfirm: () => void | Promise<void>;
	} = $props();

	const resolvedTitle = $derived(title ?? getMessage('confirm_action'));
	const resolvedConfirmText = $derived(confirmText ?? getMessage('ok'));
	const resolvedCancelText = $derived(cancelText ?? getMessage('cancel'));

	let isProcessing = $state(false);

	async function handleConfirm() {
		isProcessing = true;
		try {
			await onConfirm();
			modalStore.close();
		} catch (e) {
			// Error handling is done in the parent
			modalStore.close();
		} finally {
			isProcessing = false;
		}
	}

	function handleCancel() {
		modalStore.close();
	}
</script>

<div class="modal-header">
	<h5 class="modal-title">{resolvedTitle}</h5>
	<button
		type="button"
		class="btn-close"
		aria-label={getMessage('close')}
		onclick={handleCancel}
		disabled={isProcessing}
	></button>
</div>

<div class="modal-body">
	<p class="mb-0">{message}</p>
</div>

<div class="modal-footer">
	<Button variant="secondary" onclick={handleCancel} disabled={isProcessing}>
		{resolvedCancelText}
	</Button>
	<Button variant={variant} onclick={handleConfirm} disabled={isProcessing}>
		{#if isProcessing}
			<span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
		{/if}
		{resolvedConfirmText}
	</Button>
</div>

<style>
	.modal-header,
	.modal-body,
	.modal-footer {
		padding: 1rem;
	}

	.modal-header {
		border-bottom: 1px solid #dee2e6;
	}

	.modal-footer {
		border-top: 1px solid #dee2e6;
		display: flex;
		gap: 0.5rem;
		justify-content: flex-end;
	}

	.modal-title {
		margin: 0;
		font-size: 1.25rem;
		font-weight: 500;
	}
</style>
