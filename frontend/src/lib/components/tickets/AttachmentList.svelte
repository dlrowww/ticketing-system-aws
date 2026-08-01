<script lang="ts">
	import { onMount } from 'svelte';
	import DataTable from '$lib/components/tables/base/DataTable.svelte';
	import { attachmentsTableConfig } from '$lib/components/tables/configs/attachmentsTableConfig';
	import { getMessage } from '$lib/i18n';
	import { ticketFileDownloadUrl } from '$lib/services/Tickets';
	import { fetchAllUsers } from '$lib/services/Users';
	import type { TicketFileDto } from '$lib/types/tickets';
	import type { RowActionEvent } from '$lib/types/table';
	import type { UserListItemDto } from '$lib/types/users';
	import FilePreviewModal from '$lib/components/modals/FilePreviewModal.svelte';

	let {
		files,
		ticketId,
		loading = false,
		error = null
	}: {
		files: TicketFileDto[];
		ticketId: number;
		loading?: boolean;
		error?: string | null;
	} = $props();

	let users = $state<UserListItemDto[] | null>(null);
	let loadingUsers = $state(false);

	// Preview modal state
	let showPreviewModal = $state(false);
	let previewFileUrl = $state('');
	let previewFileName = $state('');
	let previewMimeType = $state('');

	// Fetch users for name mapping (similar to TicketDetailModal pattern)
	async function loadUsers() {
		if (loadingUsers || users !== null) return;
		loadingUsers = true;
		try {
			users = await fetchAllUsers();
		} catch (e: any) {
			console.error('[AttachmentList] Failed to fetch users:', e);
			users = []; // Set empty array to prevent retries
		} finally {
			loadingUsers = false;
		}
	}

	// Map uploader user ID to name
	function getUserName(userId: number): string | undefined {
		return users?.find((u) => u.userId === userId)?.name;
	}

	// Enrich files with uploader names
	const enrichedFiles = $derived.by(() => {
		if (!files || !users) return files;
		return files.map((file) => ({
			...file,
			uploaderName: getUserName(file.uploaderUserId)
		}));
	});

	function onRowAction(e: RowActionEvent) {
		const fileId = typeof e.id === 'string' ? Number(e.id) : e.id;
		const file = files.find((f) => f.ticketFileId === fileId);
		if (!file) return;

		if (e.action === 'download') {
			if (typeof window === 'undefined') return;
			const url = ticketFileDownloadUrl(ticketId, fileId, file.downloadRoute, false);
			window.open(url, '_blank', 'noopener,noreferrer');
		} else if (e.action === 'preview') {
			const url = ticketFileDownloadUrl(ticketId, fileId, file.downloadRoute, true); // inline=true for preview
			// Set preview state to show the modal directly (not via modalStore)
			previewFileUrl = url;
			previewFileName = file.originalName;
			previewMimeType = file.contentType;
			showPreviewModal = true;
		}
	}

	function closePreview() {
		showPreviewModal = false;
	}

	// Load users on mount
	onMount(() => {
		void loadUsers();
	});
</script>

{#if files.length === 0}
	<p class="text-muted mb-0">{getMessage('ticket_attachments_empty')}</p>
{:else}
	<DataTable config={attachmentsTableConfig} data={enrichedFiles} loading={loading || loadingUsers} {error} {onRowAction} />
{/if}

{#if showPreviewModal}
	<FilePreviewModal 
		fileUrl={previewFileUrl}
		fileName={previewFileName}
		mimeType={previewMimeType}
		onClose={closePreview}
	/>
{/if}
