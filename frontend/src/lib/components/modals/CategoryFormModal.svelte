<script lang="ts">
	import { onDestroy } from 'svelte';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import { modalStore } from '$lib/stores/modal';
	import {
		createCategory,
		deleteCategory,
		getCategoryById,
		updateCategory,
		type FieldErrors
	} from '$lib/services/Categories';

	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import ConfirmModal from '$lib/components/modals/ConfirmModal.svelte';
	import LoadingOverlay from '$lib/components/ui/LoadingOverlay.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';

	let {
		categoryId,
		onClose
	}: {
		categoryId?: number;
		onClose?: () => void;
	} = $props();

	const isEdit = $derived(typeof categoryId === 'number' && Number.isFinite(categoryId));

	// Form state
	let namePl = $state('');
	let nameEn = $state('');
	let isActive = $state<boolean>(true);

	// Loading/edit state
	let loadingDetails = $state(false);
	let detailsError = $state<string | null>(null);
	let loadedForCategoryId = $state<number | null>(null);

	// Delete confirmation state
	let showDeleteConfirm = $state(false);

	// Errors
	let fieldErrors: FieldErrors = $state({});
	let formError = $state<string | null>(null);
	let submitting = $state(false);

	const abort = new AbortController();
	onDestroy(() => abort.abort());

	function close() {
		onClose?.();
	}

	function toErrorMessage(maybeCode: string): string {
		if (/^[A-Z0-9_]+$/.test(maybeCode)) {
			return getMessage(`error_code_${maybeCode}`);
		}
		return maybeCode;
	}

	const fieldErrorMessages = $derived(
		Object.fromEntries(
			Object.entries(fieldErrors).map(([field, messages]) => {
				// Backend returns string[] for field errors
				const messageArray = Array.isArray(messages) ? messages : [messages];
				return [field, messageArray.map((m) => toErrorMessage(m))];
			})
		) as FieldErrors
	);

	async function ensureListAndRefresh() {
		if ($page.url.pathname !== '/app/admin/categories') {
			await goto('/app/admin/categories');
		}
		setTimeout(() => {
			window.dispatchEvent(new CustomEvent('categories:refresh'));
		}, 0);
	}

	async function loadDetails(id: number) {
		if (loadingDetails || loadedForCategoryId === id) return;
		loadingDetails = true;
		detailsError = null;
		try {
			const details = await getCategoryById(id, fetch, abort.signal);
			namePl = details.namePl;
			nameEn = details.nameEn;
			isActive = details.isActive;
			loadedForCategoryId = id;
		} catch (e: any) {
			const msg = e?.message ?? getMessage('category_load_failed');
			detailsError = msg;
			// Error is already displayed in modal via detailsError, no toast needed
		} finally {
			loadingDetails = false;
		}
	}

	$effect(() => {
		void categoryId;
		if (isEdit && categoryId && loadedForCategoryId !== categoryId) {
			loadDetails(categoryId);
		}
		if (!isEdit) {
			loadedForCategoryId = null;
			loadingDetails = false;
			detailsError = null;
			namePl = '';
			nameEn = '';
			isActive = true;
			fieldErrors = {};
			formError = null;
		}
	});

	async function onSubmit(e: Event) {
		e.preventDefault();

		submitting = true;
		fieldErrors = {};
		formError = null;
		try {
			if (!isEdit) {
				await createCategory(
					{
						namePl: namePl.trim(),
						nameEn: nameEn.trim()
					},
					fetch,
					abort.signal
				);
				toastStore.success(getMessage('category_create_success'));
			} else {
				await updateCategory(
					categoryId!,
					{
						namePl: namePl.trim(),
						nameEn: nameEn.trim(),
						isActive
					},
					fetch,
					abort.signal
				);
				toastStore.success(getMessage('category_update_success'));
			}

			await ensureListAndRefresh();
			close();
		} catch (err: any) {
			console.log('[CategoryFormModal] Caught error:', err);
			console.log('[CategoryFormModal] Error code:', err?.code);
			console.log('[CategoryFormModal] Field errors:', err?.fieldErrors);

			const code = err?.code as string | undefined;
			const fallback = isEdit
				? getMessage('category_update_failed')
				: getMessage('category_create_failed');

			if (err?.fieldErrors) {
				fieldErrors = err.fieldErrors as FieldErrors;
				// Validation errors are displayed inline in the form
			} else {
				// Display validation or general error at the top of modal
				const msg = code ? getMessage(`error_code_${code}`) : (err?.message ?? fallback);
				console.log('[CategoryFormModal] Setting formError to:', msg);
				formError = msg;
			}
		} finally {
			submitting = false;
		}
	}

	async function onDelete() {
		if (!isEdit) return;
		showDeleteConfirm = true;
	}

	async function handleConfirmDelete() {
		showDeleteConfirm = false;
		submitting = true;
		formError = null;
		try {
			await deleteCategory(categoryId!, fetch, abort.signal);
			toastStore.success(getMessage('category_delete_success'));
			await ensureListAndRefresh();
			close();
		} catch (e: any) {
			const code = e?.code as string | undefined;
			formError = code ? getMessage(`error_code_${code}`) : getMessage('category_delete_failed');
			// Error is already displayed in modal via formError, no toast needed
		} finally {
			submitting = false;
		}
	}

	function handleCancelDelete() {
		showDeleteConfirm = false;
	}

	function handleDeleteBackdropClick(event: MouseEvent) {
		if (event.target === event.currentTarget) {
			handleCancelDelete();
		}
	}
</script>

<div class="p-3">
	<div class="d-flex align-items-start justify-content-between gap-3 mb-3">
		<div>
			{#if isEdit}
				<h2 class="h5 mb-1">{getMessage('category_edit_title')}</h2>
				<div class="text-muted small">
					{getMessage('category_edit_subtitle', { id: categoryId })}
				</div>
			{:else}
				<h2 class="h5 mb-1">{getMessage('category_create')}</h2>
				<div class="text-muted small">{getMessage('category_create_subtitle')}</div>
			{/if}
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

	<FormError message={formError ?? undefined} />

	{#if isEdit && loadingDetails}
		<div class="d-flex align-items-center gap-2 text-muted">
			<div
				class="spinner-border spinner-border-sm"
				role="status"
				aria-label={getMessage('loading')}
			></div>
			<span>{getMessage('loading')}</span>
		</div>
	{:else if isEdit && detailsError}
		<div class="alert alert-danger" role="alert">
			<i class="bi bi-x-circle-fill me-2"></i>
			{detailsError}
		</div>
	{:else}
		<form onsubmit={onSubmit} novalidate>
			<div class="row g-3">
				<div class="col-12 col-md-6">
					<label class="form-label" for="category-name-pl">
						{getMessage('category_name_pl')}
						<span class="text-danger">*</span>
					</label>
					<Input
						id="category-name-pl"
						bind:value={namePl}
						error={fieldErrorMessages.NamePl}
						placeholder={getMessage('category_name_pl_placeholder')}
						required
					/>
				</div>

				<div class="col-12 col-md-6">
					<label class="form-label" for="category-name-en">
						{getMessage('category_name_en')}
						<span class="text-danger">*</span>
					</label>
					<Input
						id="category-name-en"
						bind:value={nameEn}
						error={fieldErrorMessages.NameEn}
						placeholder={getMessage('category_name_en_placeholder')}
						required
					/>
				</div>

				{#if isEdit}
					<div class="col-12">
						<div class="form-check mt-2">
							<input
								id="category-active"
								class="form-check-input"
								type="checkbox"
								bind:checked={isActive}
							/>
							<label class="form-check-label" for="category-active">
								{getMessage('category_active')}
							</label>
						</div>
						<div class="form-text">{getMessage('category_active_help')}</div>
					</div>
				{/if}
			</div>

			<div class="d-flex justify-content-end gap-2 mt-3">
				<Button type="button" variant="outline-secondary" onclick={close}>
					{getMessage('cancel')}
				</Button>

				<Button type="submit" variant="primary" loading={submitting} disabled={submitting}>
					<i class="bi bi-check-circle me-1"></i>
					{isEdit ? getMessage('save') : getMessage('create')}
				</Button>

				{#if isEdit}
					<Button type="button" variant="danger" onclick={onDelete} disabled={submitting}>
						<i class="bi bi-trash me-1"></i>
						{getMessage('delete')}
					</Button>
				{/if}
			</div>
		</form>
	{/if}

	<LoadingOverlay show={submitting} message={getMessage('saving')} />
</div>

{#if showDeleteConfirm}
	<div class="confirm-overlay" role="presentation" onclick={handleDeleteBackdropClick}>
		<div class="confirm-dialog">
			<div class="modal-header">
				<h5 class="modal-title">{getMessage('category_delete_confirm_title')}</h5>
				<button
					type="button"
					class="btn-close"
					aria-label={getMessage('close')}
					onclick={handleCancelDelete}
				></button>
			</div>
			<div class="modal-body">
				<p class="mb-0">{getMessage('category_delete_confirm')}</p>
			</div>
			<div class="modal-footer">
				<Button variant="secondary" onclick={handleCancelDelete}>
					{getMessage('cancel')}
				</Button>
				<Button variant="danger" onclick={handleConfirmDelete}>
					<i class="bi bi-trash me-1"></i>
					{getMessage('delete')}
				</Button>
			</div>
		</div>
	</div>
{/if}

<style>
	.confirm-overlay {
		position: fixed;
		top: 0;
		left: 0;
		right: 0;
		bottom: 0;
		background: rgba(0, 0, 0, 0.5);
		display: flex;
		align-items: center;
		justify-content: center;
		z-index: 1060;
	}

	.confirm-dialog {
		background: white;
		border-radius: 0.5rem;
		max-width: 500px;
		width: 90%;
		box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
	}

	.modal-header,
	.modal-body,
	.modal-footer {
		padding: 1rem;
	}

	.modal-header {
		border-bottom: 1px solid #dee2e6;
		display: flex;
		align-items: center;
		justify-content: space-between;
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
