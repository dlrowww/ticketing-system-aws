<script lang="ts">
	import { onDestroy } from 'svelte';
	import { get } from 'svelte/store';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import { createTicket, type FieldErrors } from '$lib/services/Tickets';
	import { lookups, toOptions } from '$lib/lookups/Lookups';
	import { locale as _locale } from 'svelte-i18n';
	import { categoryMap } from '$lib/stores/categories';

	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Textarea from '$lib/components/ui/Textarea.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import LoadingOverlay from '$lib/components/ui/LoadingOverlay.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';
	import FileListInput from '$lib/components/ui/FileListInput.svelte';

	let { onClose }: { onClose?: () => void } = $props();

	// Form state
	let title = $state('');
	let description = $state('');
	let category: number | '' = $state('');
	let priority: number | '' = $state('');
	let files: File[] = $state([]);

	// Errors from BE (400) and quick client checks
	let fieldErrors: FieldErrors = $state({});
	let formError = $state('');
	let submitting = $state(false);

	function toErrorMessage(maybeCode: string): string {
		// Backend error codes are stable, machine-readable, and uppercase (e.g. TICKET_TITLE_TOO_SHORT).
		// Client-side validation already uses localized strings.
		if (/^[A-Z0-9_]+$/.test(maybeCode)) {
			return getMessage(`error_code_${maybeCode}`);
		}
		return maybeCode;
	}

	const fieldErrorMessages = $derived(
		Object.fromEntries(
			Object.entries(fieldErrors).map(([field, messages]) => [
				field,
				(messages ?? []).map((m) => toErrorMessage(m))
			])
		) as FieldErrors
	);

	// Category options from categories store (DB-driven)
	const categoryOptions = $derived.by(() => {
		const lang = $_locale?.split('-')[0] || 'en';
		return Array.from(get(categoryMap).values()).map((cat) => ({
			value: cat.categoryId,
			labelKey: lang === 'pl' ? cat.namePl : cat.nameEn
		}));
	});
	const priorityOptions = $derived(toOptions(lookups.priority()));

	const MAX_FILES = 10;
	const MAX_FILE = 20 * 1024 * 1024; // 20 MB
	const MAX_TOTAL = 50 * 1024 * 1024; // 50 MB
	const ALLOWED = new Set([
		'image/png',
		'image/jpeg',
		'application/pdf',
		'text/plain',
		'application/zip',
		'application/x-zip-compressed'
	]);

	function formatFileSize(bytes: number): string {
		if (bytes < 1024) return bytes + ' B';
		if (bytes < 1024 * 1024) return (bytes / 1024).toFixed(1) + ' KB';
		return (bytes / (1024 * 1024)).toFixed(1) + ' MB';
	}

	const attachmentHint = $derived(
		getMessage('attachment_upload_hint', {
			maxFiles: MAX_FILES,
			maxSize: formatFileSize(MAX_FILE),
			types: 'PNG, JPEG, PDF, TXT, ZIP'
		})
	);

	const isFormValid = $derived(
		title.trim().length >= 6 &&
		description.trim().length >= 20 &&
		category !== '' &&
		priority !== ''
	);

	function close() {
		onClose?.();
	}

	function validate(): boolean {
		fieldErrors = {};

		const trimmedTitle = title.trim();
		const trimmedDescription = description.trim();

		if (!trimmedTitle || trimmedTitle.length < 6 || trimmedTitle.length > 120) {
			fieldErrors['Title'] = [
				getMessage('validation_ticket_title_length', { min: 6, max: 120 })
			];
		}

		if (!trimmedDescription || trimmedDescription.length < 20 || trimmedDescription.length > 5000) {
			fieldErrors['Description'] = [
				getMessage('validation_ticket_description_length', { min: 20, max: 5000 })
			];
		}

		if (category === '') {
			fieldErrors['Category'] = [getMessage('validation_category_required')];
		}

		if (priority === '') {
			fieldErrors['Priority'] = [getMessage('validation_priority_required')];
		}

		if (files.length > MAX_FILES) {
			fieldErrors['Files'] = [getMessage('validation_files_too_many', { max: MAX_FILES })];
		}

		if (files.length > 0) {
			const tooBig = files.find((f) => f.size > MAX_FILE);
			const badType = files.find((f) => !ALLOWED.has(f.type || 'application/octet-stream'));
			const total = files.reduce((sum, f) => sum + f.size, 0);

			if (tooBig) {
				fieldErrors['Files'] = [
					...(fieldErrors['Files'] ?? []),
					getMessage('validation_files_each_too_large', {
						maxMb: Math.round(MAX_FILE / 1024 / 1024)
					})
				];
			}

			if (badType) {
				fieldErrors['Files'] = [
					...(fieldErrors['Files'] ?? []),
					getMessage('validation_files_unsupported_type', {
						type: badType.type || 'unknown'
					})
				];
			}

			if (total > MAX_TOTAL) {
				fieldErrors['Files'] = [
					...(fieldErrors['Files'] ?? []),
					getMessage('validation_files_total_too_large', {
						maxMb: Math.round(MAX_TOTAL / 1024 / 1024)
					})
				];
			}
		}

		return Object.keys(fieldErrors).length === 0;
	}

	async function onSubmit(e: Event) {
		e.preventDefault();
		formError = '';
		if (!validate()) return;

		const fd = new FormData();
		fd.set('Title', title.trim());
		fd.set('Description', description.trim());
		fd.set('CategoryId', String(category));
		fd.set('Priority', String(priority));
		for (const f of files) fd.append('Files', f, f.name);

		submitting = true;
		try {
			await createTicket(fd);

			toastStore.success(getMessage('ticket_create_success'));

			// Ensure the user is on the tickets list, then refresh it.
			if ($page.url.pathname !== '/app/tickets') {
				await goto('/app/tickets');
			}

			// Fire an app-level refresh signal for the tickets table.
			setTimeout(() => {
				window.dispatchEvent(new CustomEvent('tickets:refresh'));
			}, 0);

			close();
		} catch (err: any) {
			if (err?.fieldErrors) {
				fieldErrors = err.fieldErrors as FieldErrors;
				toastStore.error(getMessage('ticket_create_fix_errors'));
			} else {
				formError = err?.message ?? getMessage('ticket_create_unexpected_error');
				toastStore.error(formError);
			}
		} finally {
			submitting = false;
		}
	}

	function onCancel() {
		close();
	}

	// Defensive: if this modal is destroyed while submitting, don't leave stale state.
	onDestroy(() => {
		submitting = false;
	});
</script>

<div class="p-3">
	<div class="d-flex align-items-start justify-content-between gap-3 mb-3">
		<div>
			<h2 class="h5 mb-1">{getMessage('ticket_create_modal_title')}</h2>
			<div class="text-muted small">{getMessage('ticket_create_modal_subtitle')}</div>
		</div>

		<button
			type="button"
			class="btn btn-sm btn-outline-secondary"
			onclick={close}
			aria-label={getMessage('close')}
			title={getMessage('close')}
		>
			<i class="bi bi-x-lg"></i>
		</button>
	</div>

	<FormError message={formError} />

	<form onsubmit={onSubmit} novalidate>
		<div class="mb-3">
			<label for="ticket-title" class="form-label">
				{getMessage('title')} <span class="text-danger">*</span>
			</label>
			<Input
				id="ticket-title"
				name="Title"
				maxlength={120}
				required
				bind:value={title}
				error={fieldErrorMessages['Title']}
			/>
			<div class="form-text">{getMessage('ticket_title_help', { min: 6, max: 120 })}</div>
		</div>

		<div class="mb-3">
			<label for="ticket-description" class="form-label">
				{getMessage('description')} <span class="text-danger">*</span>
			</label>
			<Textarea
				id="ticket-description"
				name="Description"
				rows={6}
				maxlength={5000}
				required
				bind:value={description}
				error={fieldErrorMessages['Description']}
			/>
			<div class="form-text">{getMessage('ticket_description_help', { min: 20, max: 5000 })}</div>
		</div>

		<div class="row g-3 mb-3">
			<div class="col-12 col-md-6">
				<label for="ticket-category" class="form-label">
					{getMessage('category')} <span class="text-danger">*</span>
				</label>
				<Select
					id="ticket-category"
					name="Category"
					required
					bind:value={category}
					options={categoryOptions}
					placeholder={getMessage('choose')}
					error={fieldErrorMessages['Category']}
				/>
			</div>

			<div class="col-12 col-md-6">
				<label for="ticket-priority" class="form-label">
					{getMessage('priority')} <span class="text-danger">*</span>
				</label>
				<Select
					id="ticket-priority"
					name="Priority"
					required
					bind:value={priority}
					options={priorityOptions}
					placeholder={getMessage('choose')}
					error={fieldErrorMessages['Priority']}
				/>
			</div>
		</div>

		<FileListInput
			id="ticket-files"
			name="Files"
			label={getMessage('attachments')}
			hint={attachmentHint}
			accept=".png,.jpg,.jpeg,.pdf,.txt,.zip"
			bind:value={files}
			error={fieldErrorMessages['Files']}
			disabled={submitting}
		/>

		<div class="d-flex justify-content-end gap-2">
			<Button variant="outline-secondary" type="button" onclick={onCancel} disabled={submitting}>
				{getMessage('cancel')}
			</Button>
			<Button variant="primary" type="submit" loading={submitting} disabled={submitting || !isFormValid}>
				{getMessage('ticket_create_submit')}
			</Button>
		</div>
	</form>
	
	<LoadingOverlay show={submitting} message={getMessage('ticket_create_submitting')} />
</div>
