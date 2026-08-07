<script lang="ts">
	import { onDestroy } from 'svelte';
	import { get } from 'svelte/store';
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';

	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import {
		createUser,
		deleteUser,
		getUserById,
		updateUser,
		type FieldErrors
	} from '$lib/services/Users';
	import { lookups, toOptions } from '$lib/lookups/Lookups';
	import { UserRole } from '$lib/types/enums';
	import { getCategoryName, categoryMap } from '$lib/stores/categories';

	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import PasswordInput from '$lib/components/ui/PasswordInput.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import LoadingOverlay from '$lib/components/ui/LoadingOverlay.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';

	let {
		userId,
		onClose
	}: {
		userId?: number;
		onClose?: () => void;
	} = $props();

	const isEdit = $derived(typeof userId === 'number' && Number.isFinite(userId));

	// Form state
	let name = $state('');
	let email = $state('');
	let password = $state('');
	let role = $state<UserRole>(UserRole.Employee);
	let categoryId = $state<number | undefined>(undefined);
	let isActive = $state<boolean>(true);

	// Loading/edit state
	let loadingDetails = $state(false);
	let detailsError = $state<string | null>(null);
	let loadedForUserId = $state<number | null>(null);

	// Errors
	let fieldErrors: FieldErrors = $state({});
	let formError = $state<string | null>(null);
	let submitting = $state(false);

	const abort = new AbortController();
	onDestroy(() => abort.abort());

	function close() {
		onClose?.();
	}

	function shouldRequireCategory(r: UserRole): boolean {
		return r === UserRole.Support || r === UserRole.TeamLeader;
	}

	// Form validation
	const isFormValid = $derived(
		name.trim().length >= 2 &&
			email.trim().length >= 5 &&
			email.includes('@') &&
			(isEdit || password.length >= 8) && // Password required only in create mode
			role !== undefined &&
			(!shouldRequireCategory(role) || categoryId !== undefined) // Category required for Support/TeamLeader
	);

	function toErrorMessage(maybeCode: string): string {
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

	const roleOptions = $derived(toOptions(lookups.userRole()));
	const categoryOptions = $derived(
		Array.from(get(categoryMap).values()).map((cat) => ({
			value: cat.categoryId,
			labelKey: getCategoryName(cat.categoryId)
		}))
	);

	async function ensureListAndRefresh() {
		if ($page.url.pathname !== '/app/admin/users') {
			await goto('/app/admin/users');
		}
		setTimeout(() => {
			window.dispatchEvent(new CustomEvent('users:refresh'));
		}, 0);
	}

	async function loadDetails(id: number) {
		if (loadingDetails || loadedForUserId === id) return;
		loadingDetails = true;
		detailsError = null;
		try {
			const details = await getUserById(id, fetch, abort.signal);
			name = details.name;
			email = details.email;
			role = details.role as any;
			categoryId = details.categoryId ?? undefined;
			isActive = details.isActive;
			loadedForUserId = id;
		} catch (e: any) {
			const msg = e?.message ?? getMessage('user_load_failed');
			detailsError = msg;
			// Error displayed in modal, no toast needed (consistent with CategoryFormModal)
		} finally {
			loadingDetails = false;
		}
	}

	$effect(() => {
		void userId;
		if (isEdit && userId && loadedForUserId !== userId) {
			loadDetails(userId);
		}
		if (!isEdit) {
			loadedForUserId = null;
			loadingDetails = false;
			detailsError = null;
			name = '';
			email = '';
			password = '';
			role = UserRole.Employee;
			categoryId = undefined;
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
				await createUser(
					{
						name: name.trim(),
						email: email.trim(),
						password,
						role,
						categoryId: categoryId ?? null
					},
					fetch,
					abort.signal
				);
				toastStore.success(getMessage('user_create_success'));
			} else {
				await updateUser(
					userId!,
					{
						name: name.trim(),
						email: email.trim(),
						password: password.trim() ? password : null,
						role,
						categoryId: categoryId ?? null,
						isActive
					},
					fetch,
					abort.signal
				);
				toastStore.success(getMessage('user_update_success'));
			}

			await ensureListAndRefresh();
			close();
		} catch (err: any) {
			if (err?.fieldErrors) {
				fieldErrors = err.fieldErrors as FieldErrors;
				// Collect all error messages from all fields
				const allErrors: string[] = [];
				for (const [field, codes] of Object.entries(fieldErrors)) {
					if (codes && codes.length > 0) {
						for (const code of codes) {
							allErrors.push(toErrorMessage(code));
						}
					}
				}
				formError = allErrors.length > 0 ? allErrors.join(' ') : getMessage('validation_failed');
			} else {
				const code = err?.code as string | undefined;
				const fallback = isEdit
					? getMessage('user_update_failed')
					: getMessage('user_create_failed');
				const msg = code ? getMessage(`error_code_${code}`) : (err?.message ?? fallback);
				formError = msg;
				toastStore.error(msg);
			}
		} finally {
			submitting = false;
		}
	}

	async function onDeactivate() {
		if (!isEdit) return;
		submitting = true;
		formError = null;
		try {
			await deleteUser(userId!, fetch, abort.signal);
			toastStore.success(getMessage('user_deactivate_success'));
			await ensureListAndRefresh();
			close();
		} catch (e: any) {
			const code = e?.code as string | undefined;
			formError = code ? getMessage(`error_code_${code}`) : getMessage('user_deactivate_failed');
			// Error displayed in modal, no toast needed
		} finally {
			submitting = false;
		}
	}
</script>

<div class="p-3">
	<div class="d-flex align-items-start justify-content-between gap-3 mb-3">
		<div>
			{#if isEdit}
				<h2 class="h5 mb-1">{getMessage('user_edit_title')}</h2>
				<div class="text-muted small">{getMessage('user_edit_subtitle', { id: userId })}</div>
			{:else}
				<h2 class="h5 mb-1">{getMessage('user_create')}</h2>
				<div class="text-muted small">{getMessage('user_create_subtitle')}</div>
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
					<label class="form-label" for="user-name">
						{getMessage('name')} <span class="text-danger">*</span>
					</label>
					<Input id="user-name" bind:value={name} error={fieldErrorMessages.Name} />
				</div>

				<div class="col-12 col-md-6">
					<label class="form-label" for="user-email">
						{getMessage('email')} <span class="text-danger">*</span>
					</label>
					<Input
						id="user-email"
						type="email"
						bind:value={email}
						error={fieldErrorMessages.Email}
						placeholder={getMessage('email_placeholder')}
					/>
				</div>

				<div class="col-12 col-md-6">
					<label class="form-label" for="user-password">
						{#if isEdit}
							{getMessage('password_new_optional')}
						{:else}
							{getMessage('password')} <span class="text-danger">*</span>
						{/if}
					</label>
					<PasswordInput
						id="user-password"
						bind:value={password}
						autocomplete="new-password"
						error={fieldErrorMessages.Password}
					/>
					<div class="form-text">{getMessage('password_min_length')}</div>
				</div>

				<div class="col-12 col-md-6">
					<label class="form-label" for="user-role">
						{getMessage('role')} <span class="text-danger">*</span>
					</label>
					<Select
						id="user-role"
						bind:value={role}
						options={roleOptions}
						error={fieldErrorMessages.Role}
					/>
				</div>

				<div class="col-12 col-md-6">
					<label class="form-label" for="user-category">
						{getMessage('category')}
						{#if shouldRequireCategory(role)}
							<span class="text-danger">*</span>
						{/if}
					</label>
					<Select
						id="user-category"
						bind:value={categoryId}
						options={categoryOptions}
						required={shouldRequireCategory(role)}
						disabled={!shouldRequireCategory(role)}
						error={fieldErrorMessages.CategoryId}
						placeholder={getMessage('choose')}
					/>
					<div class="form-text">{getMessage('user_category_help')}</div>
				</div>

				{#if isEdit}
					<div class="col-12 col-md-6">
						<div class="form-check mt-4">
							<input
								id="user-active"
								class="form-check-input"
								type="checkbox"
								bind:checked={isActive}
							/>
							<label class="form-check-label" for="user-active">{getMessage('user_active')}</label>
						</div>
					</div>
				{/if}
			</div>

			<div class="d-flex justify-content-end gap-2 mt-3">
				<Button type="button" variant="outline-secondary" onclick={close}>
					{getMessage('cancel')}
				</Button>

				<Button
					type="submit"
					variant="primary"
					loading={submitting}
					disabled={submitting || !isFormValid}
				>
					<i class="bi bi-check-circle me-1"></i>
					{isEdit ? getMessage('save') : getMessage('create')}
				</Button>

				{#if isEdit}
					<Button type="button" variant="danger" onclick={onDeactivate} disabled={submitting}>
						<i class="bi bi-person-x me-1"></i>
						{getMessage('user_deactivate')}
					</Button>
				{/if}
			</div>
		</form>
	{/if}

	<LoadingOverlay show={submitting} message={getMessage('saving')} />
</div>
