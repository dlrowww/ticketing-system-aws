<script lang="ts">
	import { getMessage } from '$lib/i18n';

	let {
		id = 'file-input',
		name = 'files',
		label,
		hint,
		accept,
		multiple = true,
		disabled = false,
		error,
		value = $bindable([]),
		maxFiles = 10,
		maxFileSize = 20 * 1024 * 1024,
		maxTotalSize = 50 * 1024 * 1024,
		allowedTypes = new Set([
			'image/png',
			'image/jpeg',
			'application/pdf',
			'text/plain',
			'application/zip',
			'application/x-zip-compressed'
		]),
		onChange
	}: {
		id?: string;
		name?: string;
		label?: string;
		hint?: string;
		accept?: string;
		multiple?: boolean;
		disabled?: boolean;
		error?: string[] | null;
		value?: File[];
		maxFiles?: number;
		maxFileSize?: number;
		maxTotalSize?: number;
		allowedTypes?: Set<string>;
		onChange?: (files: File[]) => void;
	} = $props();

	let fileInput: HTMLInputElement | undefined = $state();

	function formatFileSize(bytes: number): string {
		if (bytes < 1024) return bytes + ' B';
		if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
		return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
	}

	function handleFileChange(e: Event) {
		const input = e.target as HTMLInputElement;
		const newFiles = Array.from(input.files || []);
		value = newFiles;
		onChange?.(newFiles);
	}

	function removeFile(index: number) {
		value = value.filter((_, i) => i !== index);

		// Reset file input to sync with state
		if (fileInput) {
			fileInput.value = '';
		}

		onChange?.(value);
	}

	const hasError = $derived(error && error.length > 0);
</script>

<div class="mb-2">
	<!-- <div class="d-flex justify-content-between align-items-center mb-2">
		{#if label}
			<label for={id} class="form-label mb-0">{label}</label>
		{/if}
		{#if value.length > 0}
			<strong>{getMessage('attachment_selected_files')} ({value.length}):</strong>
		{/if}
	</div> -->

	<div class="row g-3">
		<div class="col-12 {value.length > 0 ? 'col-md-6' : ''}">
			{#if label}
				<label for={id} class="form-label">{label}</label>
			{/if}
			<input
				bind:this={fileInput}
				{id}
				{name}
				type="file"
				class="form-control {hasError ? 'is-invalid' : ''}"
				{multiple}
				{accept}
				{disabled}
				onchange={handleFileChange}
			/>

			{#if hint}
				<div class="form-text mt-1">{hint}</div>
			{/if}

			{#if hasError}
				<div class="invalid-feedback d-block">
					{#each error ?? [] as message}
						<div>{message}</div>
					{/each}
				</div>
			{/if}
		</div>

		{#if value.length > 0}
			<div class="col-12 col-md-6">
				<strong>{getMessage('attachment_selected_files')} ({value.length}):</strong>
				<div class="selected-files-container">
					<ul class="list-group list-group-flush">
						{#each value as file, index}
							<li class="list-group-item d-flex justify-content-between align-items-center">
								<span class="file-info">
									<i class="bi bi-paperclip text-secondary me-1"></i>
									<span class="file-name">{file.name}</span>
									<small class="text-muted ms-1">({formatFileSize(file.size)})</small>
								</span>
								<button
									type="button"
									class="btn btn-link text-danger p-0 ms-2 flex-shrink-0"
									onclick={() => removeFile(index)}
									{disabled}
									aria-label={getMessage('attachment_remove_file', { name: file.name })}
								>
									<i class="bi bi-x"></i>
								</button>
							</li>
						{/each}
					</ul>
				</div>
			</div>
		{/if}
	</div>
</div>

<style lang="scss">
	.selected-files-container {
		max-height: 8em; // ~3 items height
		overflow-y: auto;
		border: 1px solid var(--bs-border-color);
		border-radius: 0.375rem;
		padding: 0 0.25rem;
		background-color: var(--bs-body-bg);
		margin-top: 0.3rem;
	}

	.list-group-item {
		padding: 0.375rem 0.5rem;
		border: none;
		border-bottom: 1px solid var(--bs-border-color-translucent);
		background-color: transparent;

		&:last-child {
			border-bottom: none;
		}

		.file-info {
			display: flex;
			align-items: center;
			min-width: 0;
			flex: 1;
		}

		.file-name {
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}
	}

	.btn-link.text-danger {
		transition: all 0.15s ease-in-out;

		&:hover:not(:disabled) {
			color: var(--ironpack-red);
			transform: scale(1.15);
			text-decoration: none;
		}
	}
</style>
