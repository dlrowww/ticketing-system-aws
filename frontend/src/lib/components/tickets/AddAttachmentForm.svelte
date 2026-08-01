<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import { addTicketAttachments } from '$lib/services/Tickets';
	import Button from '$lib/components/ui/Button.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';
	import FileListInput from '$lib/components/ui/FileListInput.svelte';

	let {
		ticketId,
		onAdded
	}: {
		ticketId: number;
		onAdded?: () => void;
	} = $props();

	let selectedFiles: File[] = $state([]);
	let error = $state<string | null>(null);
	let submitting = $state(false);

	// File validation constants (match backend FileUploadOptions)
	const MAX_FILES = 10;
	const MAX_FILE_SIZE_BYTES = 20 * 1024 * 1024; // 20 MB
	const MAX_TOTAL_SIZE_BYTES = 50 * 1024 * 1024; // 50 MB

	function formatFileSize(bytes: number): string {
		if (bytes < 1024) return bytes + ' B';
		if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
		return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
	}

	const attachmentHint = $derived(
		getMessage('attachment_upload_hint', {
			maxFiles: MAX_FILES,
			maxSize: formatFileSize(MAX_FILE_SIZE_BYTES),
			types: 'PNG, JPEG, PDF, TXT, ZIP'
		})
	);

	async function submit(e: Event) {
		e.preventDefault();
		if (submitting) return;

		if (selectedFiles.length === 0) {
			error = getMessage('validation_no_files_selected');
			return;
		}

		submitting = true;
		error = null;

		try {
			const fileCount = selectedFiles.length;
			await addTicketAttachments(ticketId, selectedFiles);
			selectedFiles = [];
			toastStore.success(getMessage('attachment_upload_success', { count: fileCount }));
			onAdded?.();
		} catch (e: any) {
			const msg = e?.message ?? getMessage('attachment_upload_failed');
			error = msg;
			// Only show error in modal, not as toast
		} finally {
			submitting = false;
		}
	}
</script>

<form onsubmit={submit} class="card">
	<div class="card-body d-flex flex-column">
		{#if error}
			<FormError message={error} />
		{/if}

		<div class="flex-grow-1">
			<FileListInput
				id="ticket-files"
				name="files"
				label={getMessage('attachment_add_label')}
				hint={attachmentHint}
				accept=".png,.jpg,.jpeg,.pdf,.txt,.zip"
				bind:value={selectedFiles}
				disabled={submitting}
			/>
		</div>

		<div class="d-flex justify-content-end">
			<Button 
				type="submit" 
				variant="primary" 
				size="sm" 
				disabled={submitting || selectedFiles.length === 0}
				loading={submitting}
			>
				<i class="bi bi-upload me-1"></i>
				{submitting ? getMessage('attachment_uploading') : getMessage('attachment_upload_button')}
			</Button>
		</div>
	</div>
</form>
