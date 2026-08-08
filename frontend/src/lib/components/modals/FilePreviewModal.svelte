<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import { getFileTypeInfo, isPreviewable } from '$lib/utils/fileTypes';
	import Button from '$lib/components/ui/Button.svelte';

	let {
		fileUrl,
		fileName,
		mimeType,
		onClose
	}: {
		fileUrl: string;
		fileName: string;
		mimeType: string;
		onClose?: () => void;
	} = $props();

	const fileTypeInfo = $derived(getFileTypeInfo(mimeType));
	const isImage = $derived(fileTypeInfo.type === 'image');
	const isPdf = $derived(fileTypeInfo.type === 'pdf');
	const canPreview = $derived(isPreviewable(mimeType));

	// Zoom state for images
	let zoomLevel = $state(1);

	function close() {
		onClose?.();
	}

	function handleBackdropClick(e: MouseEvent) {
		if (e.target === e.currentTarget) {
			close();
		}
	}

	function handleDialogKeydown(e: KeyboardEvent) {
		if (e.key === 'Escape') {
			close();
		}
	}

	function zoomIn() {
		zoomLevel = Math.min(zoomLevel + 0.25, 3);
	}

	function zoomOut() {
		zoomLevel = Math.max(zoomLevel - 0.25, 0.5);
	}

	function resetZoom() {
		zoomLevel = 1;
	}

	function openInNewTab() {
		if (typeof window !== 'undefined') {
			window.open(fileUrl, '_blank', 'noopener,noreferrer');
		}
	}
</script>

<div
	class="modal fade show d-block"
	tabindex="-1"
	role="dialog"
	aria-modal="true"
	onclick={handleBackdropClick}
	onkeydown={handleDialogKeydown}
>
	<div class="modal-dialog modal-xl modal-dialog-centered">
		<div class="modal-content">
			<!-- Header -->
			<div class="modal-header">
				<h5 class="modal-title">
					<i class="bi bi-eye me-2"></i>
					{getMessage('file_preview_title')}
				</h5>
				<button type="button" class="btn-close" aria-label={getMessage('close')} onclick={close}
				></button>
			</div>

			<!-- Body -->
			<div class="modal-body p-0">
				{#if !canPreview}
					<!-- Not previewable -->
					<div class="text-center py-5">
						<i class="bi bi-file-earmark-x fs-1 text-muted mb-3"></i>
						<p class="text-muted">{getMessage('preview_not_available')}</p>
						<p class="small text-muted">{fileName}</p>
						<Button variant="primary" size="sm" onclick={openInNewTab}>
							<i class="bi bi-box-arrow-up-right me-1"></i>
							{getMessage('open_in_new_tab')}
						</Button>
					</div>
				{:else if isImage}
					<!-- Image preview with zoom -->
					<div class="image-preview-container">
						<div class="image-preview-controls">
							<div class="btn-group btn-group-sm" role="group">
								<button
									type="button"
									class="btn btn-outline-secondary"
									onclick={zoomOut}
									disabled={zoomLevel <= 0.5}
									aria-label={getMessage('zoom_out')}
									title={getMessage('zoom_out')}
								>
									<i class="bi bi-zoom-out"></i>
								</button>
								<button
									type="button"
									class="btn btn-outline-secondary"
									onclick={resetZoom}
									aria-label={getMessage('zoom_reset')}
									title={getMessage('zoom_reset')}
								>
									<i class="bi bi-aspect-ratio"></i>
								</button>
								<button
									type="button"
									class="btn btn-outline-secondary"
									onclick={zoomIn}
									disabled={zoomLevel >= 3}
									aria-label={getMessage('zoom_in')}
									title={getMessage('zoom_in')}
								>
									<i class="bi bi-zoom-in"></i>
								</button>
							</div>
							<span class="zoom-level-label">{Math.round(zoomLevel * 100)}%</span>
						</div>
						<div class="image-preview-wrapper">
							<img
								src={fileUrl}
								alt={fileName}
								class="image-preview"
								style="transform: scale({zoomLevel}); transform-origin: center;"
							/>
						</div>
					</div>
				{:else if isPdf}
					<!-- PDF preview with iframe -->
					<div class="pdf-preview-container">
						<div class="pdf-preview-controls">
							<Button variant="outline-secondary" size="sm" onclick={openInNewTab}>
								<i class="bi bi-box-arrow-up-right me-1"></i>
								{getMessage('open_in_new_tab')}
							</Button>
						</div>
						<iframe src={fileUrl} title={fileName} class="pdf-preview"></iframe>
					</div>
				{/if}
			</div>

			<!-- Footer -->
			<div class="modal-footer">
				<span class="text-muted small me-auto">{fileName}</span>
				<Button variant="secondary" onclick={close}>
					{getMessage('close')}
				</Button>
			</div>
		</div>
	</div>
</div>
<div class="modal-backdrop fade show"></div>

<style>
	.modal {
		background-color: rgba(0, 0, 0, 0.5);
	}

	.image-preview-container {
		position: relative;
		background-color: #f8f9fa;
		min-height: 500px;
		max-height: 70vh;
		overflow: auto;
	}

	.image-preview-controls {
		position: sticky;
		top: 0;
		z-index: 10;
		background-color: rgba(255, 255, 255, 0.95);
		border-bottom: 1px solid #dee2e6;
		padding: 0.75rem 1rem;
		display: flex;
		justify-content: space-between;
		align-items: center;
	}

	.zoom-level-label {
		font-size: 0.875rem;
		color: #6c757d;
		min-width: 50px;
		text-align: right;
	}

	.image-preview-wrapper {
		display: flex;
		justify-content: center;
		align-items: center;
		min-height: 400px;
		padding: 2rem;
	}

	.image-preview {
		max-width: 100%;
		height: auto;
		transition: transform 0.2s ease;
		cursor: zoom-in;
	}

	.pdf-preview-container {
		position: relative;
		background-color: #f8f9fa;
	}

	.pdf-preview-controls {
		background-color: rgba(255, 255, 255, 0.95);
		border-bottom: 1px solid #dee2e6;
		padding: 0.75rem 1rem;
		display: flex;
		justify-content: flex-end;
	}

	.pdf-preview {
		width: 100%;
		height: 70vh;
		border: none;
	}

	@media (max-width: 768px) {
		.image-preview-container,
		.pdf-preview {
			max-height: 60vh;
			height: 60vh;
		}
	}
</style>
