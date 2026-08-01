<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import { formatDateTimeForDetails } from '$lib/utils/dateTime';
	import type { TicketCommentDto } from '$lib/types/tickets';
	import { UserRoleKey, type UserRole } from '$lib/types/enums';

	let {
		comments,
		loading = false,
		error = null
	}: {
		comments: TicketCommentDto[];
		loading?: boolean;
		error?: string | null;
	} = $props();

	function formatDateTime(iso: string): string {
		return formatDateTimeForDetails(iso);
	}

	function formatAuthor(c: TicketCommentDto): string {
		const name = c.createdByName ?? `#${c.createdById}`;
		const roleKey = UserRoleKey[c.createdByRoleId as UserRole];
		return `${name} (${getMessage(roleKey)})`;
	}
</script>

{#if loading}
	<div class="d-flex align-items-center gap-2 text-muted">
		<div class="spinner-border spinner-border-sm" role="status" aria-label={getMessage('loading')}></div>
		<span>{getMessage('loading')}</span>
	</div>
{:else if error}
	<div class="alert alert-danger" role="alert">
		<i class="bi bi-x-circle-fill me-2"></i>
		{error}
	</div>
{:else if comments.length === 0}
	<div class="alert alert-light" role="status">
		<i class="bi bi-chat-left me-2"></i>
		{getMessage('ticket_comments_empty')}
	</div>
{:else}
	<ul class="list-group">
		{#each comments as c (c.commentId)}
			<li class="list-group-item">
				<div class="d-flex justify-content-between align-items-start gap-3">
					<div>
						<div class="fw-semibold d-flex align-items-center gap-2 flex-wrap">
							<span>{formatAuthor(c)}</span>
							{#if c.isInternal}
								<span class="badge bg-secondary-subtle text-secondary border border-secondary-subtle fw-normal">
									{getMessage('ticket_comment_internal_badge')}
								</span>
							{/if}
						</div>
						<div class="text-muted small">{formatDateTime(c.createdAt)}</div>
					</div>
				</div>
				<div class="mt-2" style="white-space: pre-wrap;">{c.content}</div>
			</li>
		{/each}
	</ul>
{/if}
