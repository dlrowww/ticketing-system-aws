<script lang="ts">
	import { onDestroy } from 'svelte';
	import { get } from 'svelte/store';

	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import { getTicketById, listTicketFiles, listTicketHistory, listTicketComments, updateTicket, getAllowedStatuses, type UpdateTicketRequest } from '$lib/services/Tickets';
	import type { TicketDetail, TicketFileDto, TicketHistoryDto, TicketCommentDto } from '$lib/types/tickets';
	import type { User } from '$lib/types/user';
	import { fetchAssignableUsers } from '$lib/services/Users';
	import type { AssignableUserDto } from '$lib/types/users';
	import TicketStatusBadge from '$lib/components/TicketStatusBadge.svelte';
	import { priorityName, statusName, lookups } from '$lib/lookups/Lookups';
	import { UserRole } from '$lib/types/enums';
	import { getCategoryName, categoryMap } from '$lib/stores/categories';
	import { createEnsure } from '$lib/utils/createEnsure';
	import { formatDateTimeForDetails } from '$lib/utils/dateTime';
	import { computeEditCapabilities, canEditTicket } from '$lib/utils/ticketPermissions';

	import TicketHistory from '$lib/components/tickets/TicketHistory.svelte';
	import CommentList from '$lib/components/tickets/CommentList.svelte';
	import AddCommentForm from '$lib/components/tickets/AddCommentForm.svelte';
	import AttachmentList from '$lib/components/tickets/AttachmentList.svelte';
	import AddAttachmentForm from '$lib/components/tickets/AddAttachmentForm.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import Input from '$lib/components/ui/Input.svelte';
	import Textarea from '$lib/components/ui/Textarea.svelte';
	import Select from '$lib/components/ui/Select.svelte';
	import UserSelect from '$lib/components/ui/UserSelect.svelte';
	import LoadingOverlay from '$lib/components/ui/LoadingOverlay.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';

	let {
		ticketId,
		user,
		onClose,
		onTicketUpdated
	}: {
		ticketId: number;
		user: User | null | undefined;
		onClose?: () => void;
		onTicketUpdated?: () => void;
	} = $props();

	type TabKey = 'details' | 'history' | 'comments' | 'attachments';
	let activeTab = $state<TabKey>('details');

	// Edit mode state
	let isEditMode = $state(false);
	let isSaving = $state(false);
	let saveError = $state<string | null>(null);

	// Edit form fields
	let editTitle = $state('');
	let editDescription = $state('');
	let editCategory = $state<number | undefined>(undefined);
	let editPriority = $state<number | undefined>(undefined);
	let editStatus = $state<number | undefined>(undefined);
	let editAssignedToId = $state<number | null | undefined>(undefined);

	// Ticket
	let ticket = $state<TicketDetail | null>(null);
	let loadingTicket = $state(false);
	let ticketError = $state<string | null>(null);

	// History
	let history = $state<TicketHistoryDto[] | null>(null);
	let loadingHistory = $state(false);
	let historyError = $state<string | null>(null);

	// Comments
	let comments = $state<TicketCommentDto[] | null>(null);
	let loadingComments = $state(false);
	let commentsError = $state<string | null>(null);
	let commentFilter = $state<'all' | 'public' | 'internal'>('all');

	// Attachments
	let files = $state<TicketFileDto[] | null>(null);
	let loadingFiles = $state(false);
	let filesError = $state<string | null>(null);

	// Assignable users (for edit mode assignment dropdown)
	let assignableUsers = $state<AssignableUserDto[] | null>(null);
	let loadingAssignableUsers = $state(false);
	let assignableUsersError = $state<string | null>(null);

	// Allowed statuses (for edit mode status dropdown)
	let allowedStatuses = $state<number[] | null>(null);
	let loadingAllowedStatuses = $state(false);

	const abort = new AbortController();
	onDestroy(() => abort.abort());

	function close() {
		onClose?.();
	}

	function formatDateTime(iso?: string | null): string {
		return formatDateTimeForDetails(iso ?? null);
	}

	async function loadTicket() {
		if (loadingTicket) return;
		loadingTicket = true;
		ticketError = null;
		try {
			ticket = await getTicketById(ticketId, fetch, abort.signal);
		} catch (e: any) {
			const msg = e?.message ?? getMessage('ticket_detail_load_failed');
			ticketError = msg;
			toastStore.error(msg);
		} finally {
			loadingTicket = false;
		}
	}

	async function ensureHistory() {
		await ensureHistoryOnce();
	}

	async function ensureComments() {
		await ensureCommentsOnce();
	}

	async function ensureFiles() {
		await ensureFilesOnce();
	}

	const ensureHistoryOnce = createEnsure<TicketHistoryDto[]>({
		getValue: () => history,
		setValue: (v) => (history = v),
		getLoading: () => loadingHistory,
		setLoading: (v) => (loadingHistory = v),
		setError: (v) => (historyError = v),
		loader: () => listTicketHistory(ticketId, fetch, abort.signal),
		errorFallback: () => getMessage('ticket_history_load_failed')
	});

	const ensureCommentsOnce = createEnsure<TicketCommentDto[]>({
		getValue: () => comments,
		setValue: (v) => (comments = v),
		getLoading: () => loadingComments,
		setLoading: (v) => (loadingComments = v),
		setError: (v) => (commentsError = v),
		loader: () => listTicketComments(ticketId, fetch, abort.signal),
		errorFallback: () => getMessage('ticket_comments_load_failed')
	});

	const ensureFilesOnce = createEnsure<TicketFileDto[]>({
		getValue: () => files,
		setValue: (v) => (files = v),
		getLoading: () => loadingFiles,
		setLoading: (v) => (loadingFiles = v),
		setError: (v) => (filesError = v),
		loader: () => listTicketFiles(ticketId, fetch),
		errorFallback: () => getMessage('ticket_attachments_load_failed')
	});

	let lastLoadedTab = $state<TabKey | null>(null);

	function selectTab(tab: TabKey) {
		activeTab = tab;
	}

	function onCommentAdded() {
		// reset cache so we re-fetch on next ensure
		comments = null;
		void ensureComments();
		// Also refresh history to show the CommentAdded event
		history = null;
		void ensureHistory();
	}

	function onFileAdded() {
		// reset cache so we re-fetch on next ensure
		files = null;
		void ensureFiles();
		// Also refresh history to show the FileAdded events
		history = null;
		void ensureHistory();
	}

	// Compute edit capabilities
	const capabilities = $derived(ticket ? computeEditCapabilities(ticket, user) : null);
	const hasAnyEditPermission = $derived(ticket && user ? canEditTicket(ticket, user) : false);

	// Form validation for edit mode
	const isEditFormValid = $derived(
		editTitle.trim().length >= 6 &&
		editDescription.trim().length >= 20 &&
		editCategory !== undefined &&
		editPriority !== undefined
	);

	// Helper to get category name by ID
	const currentCategoryName = $derived.by(() => {
		if (!ticket) return null;
		return getCategoryName(ticket.categoryId);
	});

	// Combine assignable users with current assignee (if not already included)
	// This ensures the Select component can display the current assignee
	// IMPORTANT: Use editAssignedToId (current form value), not ticket.assignedToId (original data)
	const assignableUsersWithCurrent = $derived.by(() => {
		if (!assignableUsers || !ticket) return assignableUsers ?? [];
		
		// Get the currently selected assignee from the form data
		const currentAssigneeId = editAssignedToId;
		
		// If no one is assigned, return a new array (ensure reactivity)
		if (!currentAssigneeId) return [...assignableUsers];
		
		// Check if current assignee is already in the list
		const currentAssigneeInList = assignableUsers.some(u => u.userId === currentAssigneeId);
		
		// If already included, return a new array (ensure reactivity)
		if (currentAssigneeInList) return [...assignableUsers];
		
		// Otherwise, add current assignee to the list so Select can display them
		// This only happens for the ORIGINAL assignee if backend doesn't include them
		if (currentAssigneeId === ticket.assignedToId && ticket.assignedToName) {
			// Original assignee - use ticket data
			const currentAssignee: AssignableUserDto = {
				userId: ticket.assignedToId,
				name: ticket.assignedToName,
				email: '', // Not available from ticket details
				roleName: '', // Not available from ticket details
				categoryId: null,
				categoryNamePl: null,
				categoryNameEn: null
			};
			return [currentAssignee, ...assignableUsers];
		}
		
		// Shouldn't happen: user selected someone not in list and not original assignee
		// Just return assignable users as new array
		return [...assignableUsers];
	});

	async function loadAssignableUsers() {
		if (loadingAssignableUsers) return;
		loadingAssignableUsers = true;
		assignableUsersError = null;
		try {
			assignableUsers = await fetchAssignableUsers(ticketId, fetch, abort.signal);
		} catch (e: any) {
			const msg = e?.message ?? getMessage('error_loading_users');
			assignableUsersError = msg;
			console.error('Failed to load assignable users:', e);
		} finally {
			loadingAssignableUsers = false;
		}
	}

	async function loadAllowedStatuses() {
		if (loadingAllowedStatuses || allowedStatuses) return; // Skip if already loaded
		loadingAllowedStatuses = true;
		try {
			allowedStatuses = await getAllowedStatuses(ticketId, fetch, abort.signal);
		} catch (e) {
			// Graceful fallback: If API fails, show all statuses (existing behavior)
			console.warn('Failed to load allowed statuses, showing all statuses:', e);
			allowedStatuses = lookups.ticketStatus().map(s => s.id);
		} finally {
			loadingAllowedStatuses = false;
		}
	}

	function enterEditMode() {
		if (!ticket) return;
		isEditMode = true;
		saveError = null; // Clear any previous errors
		// Populate edit form with current values
		editTitle = ticket.title;
		editDescription = ticket.description;
		editCategory = ticket.categoryId;
		editPriority = ticket.priority;
		editStatus = ticket.status;
		editAssignedToId = ticket.assignedToId;
		
		// Load assignable users if user can edit assignment
		if (capabilities?.canEditAssignment) {
			loadAssignableUsers();
		}
		
		// Load allowed statuses if user can edit status
		if (capabilities?.canEditStatus) {
			loadAllowedStatuses();
		}
	}

	function cancelEdit() {
		isEditMode = false;
		// Clear edit form
		editTitle = '';
		editDescription = '';
		editCategory = undefined;
		editPriority = undefined;
		editStatus = undefined;
		editAssignedToId = undefined;
		// Reset allowed statuses (will reload on next edit)
		allowedStatuses = null;
	}

	async function saveChanges() {
		if (!ticket || !capabilities) return;

		isSaving = true;
		saveError = null;
		try {
			const request: UpdateTicketRequest = {};

			// Only include fields that changed AND user has permission to edit
			if (capabilities.canEditTitle && editTitle !== ticket.title) {
				request.title = editTitle;
			}
			if (capabilities.canEditDescription && editDescription !== ticket.description) {
				request.description = editDescription;
			}
			if (capabilities.canEditCategory && editCategory !== ticket.categoryId) {
				request.category = editCategory ?? undefined;
			}
			if (capabilities.canEditPriority && editPriority !== ticket.priority) {
				request.priority = editPriority ?? undefined;
			}
			if (capabilities.canEditStatus && editStatus !== ticket.status) {
				request.status = editStatus ?? undefined;
			}
			if (capabilities.canEditAssignment && editAssignedToId !== ticket.assignedToId) {
				if (editAssignedToId === null) {
					request.clearAssignment = true;
				} else if (editAssignedToId !== undefined) {
				request.assignedToUserId = editAssignedToId;
			}
		}

		const updated = await updateTicket(ticketId, request, fetch, abort.signal);
		
			ticket = updated;
			isEditMode = false;
			toastStore.success(getMessage('ticket_edit_success'));

			history = null;
			void ensureHistory();

			// Notify parent that ticket was updated (so list can refresh)
			onTicketUpdated?.();
		} catch (e: any) {
			const errorCode = e?.code;
			if (e?.fieldErrors) {
				const allErrors: string[] = [];
				for (const [field, codes] of Object.entries(e.fieldErrors)) {
					if (codes && Array.isArray(codes) && codes.length > 0) {
						for (const code of codes) {
							if (/^[A-Z0-9_]+$/.test(code)) {
								allErrors.push(getMessage(`error_code_${code}`));
							} else {
								allErrors.push(code);
							}
						}
					}
				}
				saveError = allErrors.length > 0 ? allErrors.join(' ') : getMessage('validation_failed');
			} else if (errorCode) {
				const i18nKey = `error_code_${errorCode}`;
				const translatedMsg = getMessage(i18nKey);
				saveError = translatedMsg !== i18nKey ? translatedMsg : (e?.message ?? getMessage('ticket_edit_failed'));
			} else {
				saveError = e?.message ?? getMessage('ticket_edit_failed');
			}
		} finally {
			isSaving = false;
		}
	}

	$effect(() => {
		void ticketId;

		// Always load ticket basics.
		if (!ticket && !loadingTicket && !ticketError) {
			loadTicket();
		}
	});

	$effect(() => {
		void activeTab;

		// Only trigger tab-specific loaders when the tab actually changes.
		// This prevents request storms (and resulting 429s) caused by redundant reruns.
		if (lastLoadedTab === activeTab) return;
		lastLoadedTab = activeTab;

		if (activeTab === 'history') {
			void ensureHistory();
		}
		if (activeTab === 'comments') {
			void ensureComments();
		}
		if (activeTab === 'attachments') void ensureFiles();
	});

	const canSeeInternalComments = $derived.by(() => {
		const roleId = Number(user?.roleId);
		return roleId === UserRole.Admin || roleId === UserRole.TeamLeader || roleId === UserRole.Support;
	});

	const commentFilterOptions = [
		{ value: 'all', labelKey: 'ticket_comment_filter_all' },
		{ value: 'public', labelKey: 'ticket_comment_filter_public' },
		{ value: 'internal', labelKey: 'ticket_comment_filter_internal' }
	] satisfies Array<{ value: string; labelKey: string }>;

	const filteredComments = $derived.by(() => {
		const list = comments ?? [];
		if (!canSeeInternalComments) return list;
		if (commentFilter === 'public') return list.filter((c) => !c.isInternal);
		if (commentFilter === 'internal') return list.filter((c) => c.isInternal);
		return list;
	});
</script>

<div class="p-3">
	<div class="d-flex align-items-start justify-content-between gap-3 mb-3">
		<div>
			<h2 class="h5 mb-1">{getMessage('ticket_detail_modal_title', { id: ticketId })}</h2>
			{#if ticket}
				<div class="d-flex align-items-center gap-2 flex-wrap">
					<div class="text-muted small">{ticket.title}</div>
					<TicketStatusBadge status={ticket.status} />
				</div>
			{:else if loadingTicket}
				<div class="text-muted small">{getMessage('loading')}</div>
			{:else if ticketError}
				<div class="text-danger small">{ticketError}</div>
			{/if}
		</div>

		<Button
			type="button"
			variant="outline-secondary"
			size="sm"
			onclick={close}
			class="px-2"
		>
			<i class="bi bi-x-lg"></i>
			<span class="visually-hidden">{getMessage('close')}</span>
		</Button>
	</div>

	<ul class="nav nav-tabs" role="tablist">
		<li class="nav-item" role="presentation">
			<a
				href="#ticket-tab-details"
				class="nav-link {activeTab === 'details' ? 'active' : ''}"
				role="tab"
				aria-selected={activeTab === 'details'}
				onclick={(e) => {
					e.preventDefault();
					selectTab('details');
				}}
			>
				<i class="bi bi-card-text me-1"></i>
				{getMessage('ticket_tab_details')}
			</a>
		</li>
		<li class="nav-item" role="presentation">
			<a
				href="#ticket-tab-history"
				class="nav-link {activeTab === 'history' ? 'active' : ''}"
				role="tab"
				aria-selected={activeTab === 'history'}
				onclick={(e) => {
					e.preventDefault();
					selectTab('history');
				}}
			>
				<i class="bi bi-clock-history me-1"></i>
				{getMessage('ticket_tab_history')}
			</a>
		</li>
		<li class="nav-item" role="presentation">
			<a
				href="#ticket-tab-comments"
				class="nav-link {activeTab === 'comments' ? 'active' : ''}"
				role="tab"
				aria-selected={activeTab === 'comments'}
				onclick={(e) => {
					e.preventDefault();
					selectTab('comments');
				}}
			>
				<i class="bi bi-chat-left-text me-1"></i>
				{getMessage('ticket_tab_comments')}
			</a>
		</li>
		<li class="nav-item" role="presentation">
			<a
				href="#ticket-tab-attachments"
				class="nav-link {activeTab === 'attachments' ? 'active' : ''}"
				role="tab"
				aria-selected={activeTab === 'attachments'}
				onclick={(e) => {
					e.preventDefault();
					selectTab('attachments');
				}}
			>
				<i class="bi bi-paperclip me-1"></i>
				{getMessage('ticket_tab_attachments')}
			</a>
		</li>
	</ul>

	<!-- Edit Mode Validation Error -->
	{#if saveError}
		<div class="mt-3">
			<FormError message={saveError} />
		</div>
	{/if}

	<!-- Tab Content Container (scrollable) -->
	<div class="tab-content-container pt-3">
		{#if activeTab === 'details'}
			{#if loadingTicket}
				<div class="d-flex align-items-center gap-2 text-muted">
					<div class="spinner-border spinner-border-sm" role="status" aria-label={getMessage('loading')}></div>
					<span>{getMessage('loading')}</span>
				</div>
			{:else if ticketError}
				<div class="alert alert-danger" role="alert">
					<i class="bi bi-x-circle-fill me-2"></i>
					{ticketError}
				</div>
			{:else if ticket && capabilities}
				{#if isSaving}
					<LoadingOverlay message={getMessage('saving')} />
				{/if}
				
				<div class="row g-3">
					<div class="col-12">
						<div class="card details-upper-part">
							<div class="card-body">
								{#if isEditMode}
									<!-- Edit Mode: Title -->
									<div class="mb-3">
										<label for="edit-title" class="form-label text-muted small">
											{getMessage('title')}
											{#if !capabilities.canEditTitle}
												<i class="bi bi-lock-fill ms-1" title={getMessage('ticket_edit_field_disabled')}></i>
											{/if}
										</label>
										<Input
											id="edit-title"
											bind:value={editTitle}
											disabled={!capabilities.canEditTitle}
											placeholder={getMessage('title')}
											maxlength={200}
										/>
									</div>

									<!-- Edit Mode: Description -->
									<div class="mb-0">
										<label for="edit-description" class="form-label text-muted small">
											{getMessage('description')}
											{#if !capabilities.canEditDescription}
												<i class="bi bi-lock-fill ms-1" title={getMessage('ticket_edit_field_disabled')}></i>
											{/if}
										</label>
										<Textarea
											id="edit-description"
											bind:value={editDescription}
											disabled={!capabilities.canEditDescription}
											placeholder={getMessage('description')}
											rows={4}
											maxlength={5000}
										/>
									</div>
								{:else}
									<!-- View Mode: Title -->
									<div class="mb-3">
										<div class="text-muted small mb-1">{getMessage('title')}</div>
										<div class="fw-semibold">{ticket.title}</div>
									</div>

									<!-- View Mode: Description -->
									<div class="mb-0">
										<div class="text-muted small mb-1">{getMessage('description')}</div>
										<pre class="mb-0" style="white-space: pre-wrap;">{ticket.description}</pre>
									</div>
								{/if}
							</div>
						</div>
					</div>

					<div class="col-12">
						<div class="card details-lower-part">
							<div class="card-body d-flex flex-column">
								<div class="row g-3">
									<!-- Category -->
									<div class="col-12 col-md-4">
										{#if isEditMode}
											<label for="edit-category" class="form-label text-muted small">
												{getMessage('category')}
												{#if !capabilities.canEditCategory}
													<i class="bi bi-lock-fill ms-1" title={getMessage('ticket_edit_field_disabled')}></i>
												{/if}
											</label>
											<Select
												id="edit-category"
												bind:value={editCategory}
												disabled={!capabilities.canEditCategory}
												options={Array.from(get(categoryMap).values()).map(c => ({
													value: c.categoryId,
													labelKey: getCategoryName(c.categoryId)
												}))}
											/>
										{:else}
											<div class="text-muted small mb-1">{getMessage('category')}</div>
											<div>{currentCategoryName ?? getMessage('not_available')}</div>
										{/if}
									</div>

									<!-- Priority -->
									<div class="col-12 col-md-4">
										{#if isEditMode}
											<label for="edit-priority" class="form-label text-muted small">
												{getMessage('priority')}
												{#if !capabilities.canEditPriority}
													<i class="bi bi-lock-fill ms-1" title={getMessage('ticket_edit_field_disabled')}></i>
												{/if}
											</label>
											<Select
												id="edit-priority"
												bind:value={editPriority}
												disabled={!capabilities.canEditPriority}
												options={lookups.priority().map(p => ({ value: p.id, label: getMessage(p.name) }))}
											/>
										{:else}
											<div class="text-muted small mb-1">{getMessage('priority')}</div>
											<div>{getMessage(priorityName(ticket.priority) ?? 'not_available')}</div>
										{/if}
									</div>

									<!-- Status -->
									<div class="col-12 col-md-4">
										{#if isEditMode}
											<label for="edit-status" class="form-label text-muted small">
												{getMessage('status')}
												{#if !capabilities.canEditStatus}
													<i class="bi bi-lock-fill ms-1" title={getMessage('ticket_edit_field_disabled')}></i>
												{/if}
											</label>
											<Select
												id="edit-status"
												bind:value={editStatus}
												disabled={!capabilities.canEditStatus}
												options={lookups.ticketStatus()
													.filter(s => allowedStatuses?.includes(s.id) ?? true)
													.map(s => ({ value: s.id, label: getMessage(s.name) }))}
											/>
										{:else}
											<div class="text-muted small mb-1">{getMessage('status')}</div>
											<div>{getMessage(statusName(ticket.status) ?? 'not_available')}</div>
										{/if}
									</div>
								</div>

								<hr class="my-3" />

								<div class="row g-3">
									<div class="col-12 col-md-6">
										<div class="text-muted small mb-1">{getMessage('created_by')}</div>
										<div>{ticket.createdByName ?? `#${ticket.createdById}`}</div>
									</div>
									<div class="col-12 col-md-6">
										{#if isEditMode && capabilities.canEditAssignment}
											<label for="edit-assignee" class="form-label text-muted small">
												{getMessage('assigned_to')}
											</label>
										{#if assignableUsersError}
											<FormError message={assignableUsersError} />
										{:else}
											<UserSelect
												ticketId={ticketId}
												excludeUserIds={ticket?.assignedToId ? [ticket.assignedToId] : []}
												id="edit-assignee"
												bind:value={editAssignedToId}
												users={assignableUsersWithCurrent}
												loading={loadingAssignableUsers}
												disabled={isSaving}
											/>
										{/if}
										{:else}
											<div class="text-muted small mb-1">{getMessage('assigned_to')}</div>
											<div>
												{ticket.assignedToName ?? (ticket.assignedToId ? `#${ticket.assignedToId}` : getMessage('not_assigned'))}
											</div>
										{/if}
									</div>
								</div>

								<hr class="my-3" />

								<div class="row g-3 flex-grow-1">
									<div class="col-12 col-md-6">
										<div class="text-muted small mb-1">{getMessage('createdAt')}</div>
										<div>{formatDateTime(ticket.createdAt)}</div>
									</div>
									<div class="col-12 col-md-6">
										<div class="text-muted small mb-1">{getMessage('updatedAt')}</div>
										<div>{formatDateTime(ticket.updatedAt ?? null)}</div>
									</div>
								</div>

								{#if isEditMode}
									<!-- Edit Mode Buttons -->
									<div class="d-flex justify-content-end gap-2 mt-3">
										<Button
											type="button"
											variant="outline-secondary"
											size="sm"
											onclick={cancelEdit}
											disabled={isSaving}
										>
											<i class="bi bi-x-circle me-1"></i>
											{getMessage('ticket_edit_cancel')}
										</Button>
										<Button
											type="button"
											variant="primary"
											size="sm"
											onclick={saveChanges}
											loading={isSaving}
											disabled={isSaving || !isEditFormValid}
										>
											<i class="bi bi-check-circle me-1"></i>
											{getMessage('ticket_edit_save')}
										</Button>
									</div>
								{:else}
									<!-- View Mode Button -->
									<div class="d-flex justify-content-end mt-3">
										{#if hasAnyEditPermission}
											<Button
												type="button"
												variant="outline-primary"
												size="sm"
												onclick={enterEditMode}
											>
												<i class="bi bi-pencil-square me-1"></i>
												{getMessage('ticket_edit')}
											</Button>
										{:else}
											<Button
												type="button"
												variant="outline-secondary"
												size="sm"
												disabled
											>
												<i class="bi bi-lock-fill me-1"></i>
												{getMessage('ticket_edit_no_permission')}
											</Button>
										{/if}
									</div>
								{/if}
							</div>
						</div>
					</div>
				</div>
			{/if}
		{:else if activeTab === 'history'}
		<div class="card">
			<div class="card-body">
			<div class="history-scrollable">
				<TicketHistory entries={history ?? []} loading={loadingHistory} error={historyError} />
			</div>
			</div>
		</div>
		{:else if activeTab === 'comments'}
			<div class="d-flex flex-column">
				<!-- Comment form stays at top (fixed height) -->
				<div class="comments-top-section">
					<AddCommentForm ticketId={ticketId} onAdded={onCommentAdded} canMarkInternal={canSeeInternalComments} />
					{#if canSeeInternalComments}
						<div class="d-flex align-items-center mt-3">
							<div class="d-flex align-items-center gap-2">
								<span class="text-muted small">{getMessage('ticket_comment_filter_label')}</span>
								<Select bind:value={commentFilter} options={commentFilterOptions} class="form-select-sm" />
							</div>
						</div>
					{/if}
				</div>
				<!-- Comments list is scrollable (wrapped in card) -->
				<div class="card mt-3">
					<div class="card-body">
						<div class="comments-scrollable">
							<CommentList comments={filteredComments} loading={loadingComments} error={commentsError} />
						</div>
					</div>
				</div>
			</div>
		{:else if activeTab === 'attachments'}
			<div class="d-flex flex-column">
				<!-- Attachment form stays at top (fixed height with micro-scroll for file list) -->
				<div class="attachments-top-section">
					<AddAttachmentForm ticketId={ticketId} onAdded={onFileAdded} />
				</div>
				<!-- Attachments list is scrollable (wrapped in card) -->
				<div class="attachments-scrollable mt-3">
					<div class="card">
						<div class="card-body">
							<AttachmentList ticketId={ticketId} files={files ?? []} loading={loadingFiles} error={filesError} />
						</div>
					</div>
				</div>
			</div>
		{/if}
	</div>
</div>

<style lang="scss">
	.nav-link {
		color: var(--ironpack-gray-dark);
		&:hover {
			color: var(--ironpack-red);
		}
		&.active {
			font-weight: bold;
			text-decoration: underline;
		}
	}

	.details-upper-part {
		min-height: 18em; /* Ensure minimum height for upper part */
	}

	.details-lower-part {
		min-height: 20.5em; /* Ensure minimum height for upper part */
	}

	/* Tab content container with FIXED height to prevent jumping */
	/* Increased height for less scrolling - using em units for responsiveness */
	.tab-content-container {
		height: calc(90vh - 8em); /* Fixed height: 90vh minus header/tabs/padding (8em ≈ 128px) */
		overflow-x: hidden; /* Prevent horizontal scroll */
		overflow-y: auto; /* Allow vertical scroll for Details tab */
	}

	/* For tabs with specific scrollable lists, override container scroll */
	.tab-content-container:has(.history-scrollable),
	.tab-content-container:has(.comments-scrollable),
	.tab-content-container:has(.attachments-scrollable) {
		overflow-y: hidden; /* Disable container scroll when lists handle scrolling */
	}

	/* History scrollable - fills most of the available space (no forms above) */
	.history-scrollable {
		height: calc(90vh - 12em); /* Almost full height (12em ≈ 192px for header/tabs) */
		overflow-y: auto;
		overflow-x: hidden;
		padding-right: 0.5rem;
	}

	/* Comments top section - fixed height */
	.comments-top-section {
		// min-height: 10em; /* Fixed height for comment form + filter (10em ≈ 160px) */
		// max-height: 10em;
		flex-shrink: 0;
	}

	/* Comments scrollable - fills remaining space after top section */
	.comments-scrollable {
		height: calc(90vh - 30em); /* 90vh - (10em header + 10em form + 3em spacing) */
		overflow-y: auto;
		overflow-x: hidden;
		padding-right: 0.5rem;
		
		/* Card container provides borders during scroll */
		:global(.card) {
			height: 100%;
			overflow: hidden;
		}
		
		:global(.card-body) {
			height: 100%;
			overflow-y: auto;
		}
	}

	/* Attachments top section - variable height with micro-scroll for file list */
	:global(.attachments-top-section .card-body) {
		max-height: 14.5em; /* Max height for form (18em ≈ 288px) */
		min-height: 14.5em;
		overflow-y: auto; /* Micro-scroll for selected files list */
		flex-shrink: 0;
		padding-right: 0.5rem; /* Space for scrollbar if needed */
	}

	/* Attachments scrollable - fills remaining space after top section */
	.attachments-scrollable {
		height: calc(90vh - 26em); /* 90vh - (10em header + 18em form max + 3em spacing) */
		//overflow-y: auto;
		overflow-x: hidden;
		
		/* Card container provides borders during scroll */
		.card {
			height: 100%;
			overflow: hidden;
		}
		
		.card-body {
			height: 100%;
			overflow-y: auto;
		}
	}
</style>
