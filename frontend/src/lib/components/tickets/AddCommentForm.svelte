<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import { toastStore } from '$lib/stores/toast';
	import { addTicketComment } from '$lib/services/Tickets';
	import Textarea from '$lib/components/ui/Textarea.svelte';
	import Button from '$lib/components/ui/Button.svelte';

	let {
		ticketId,
		onAdded,
		canMarkInternal = false
	}: {
		ticketId: number;
		onAdded?: () => void;
		canMarkInternal?: boolean;
	} = $props();

	let content = $state('');
	let isInternal = $state(false);
	let error = $state<string | null>(null);
	let submitting = $state(false);

	function validate(): boolean {
		error = null;
		const trimmed = content.trim();
		if (!trimmed) {
			error = getMessage('validation_comment_required');
			return false;
		}
		if (trimmed.length > 5000) {
			error = getMessage('validation_comment_too_long', { max: 5000 });
			return false;
		}
		return true;
	}

	async function submit(e: Event) {
		e.preventDefault();
		if (submitting) return;
		if (!validate()) return;

		submitting = true;
		try {
			await addTicketComment(ticketId, { content: content.trim(), isInternal: canMarkInternal ? isInternal : false });
			content = '';
			isInternal = false;
			toastStore.success(getMessage('ticket_comment_added'));
			onAdded?.();
		} catch (e: any) {
			const msg = e?.message ?? getMessage('ticket_comment_add_failed');
			toastStore.error(msg);
		} finally {
			submitting = false;
		}
	}
</script>

<form onsubmit={submit} class="card">
	<div class="card-body">
		<label for="ticket-comment" class="form-label">{getMessage('ticket_add_comment_label')}</label>
		<Textarea
			id="ticket-comment"
			name="Content"
			rows={3}
			bind:value={content}
			placeholder={getMessage('ticket_add_comment_placeholder')}
			error={error ? [error] : undefined}
		/>
		{#if canMarkInternal}
			<div class="form-check mt-2">
				<input
					id="ticket-comment-internal"
					class="form-check-input"
					type="checkbox"
					bind:checked={isInternal}
					disabled={submitting}
				/>
				<label class="form-check-label" for="ticket-comment-internal">
					{getMessage('ticket_comment_internal_checkbox')}
				</label>
			</div>
		{/if}
		<div class="d-flex justify-content-end mt-2">
			<Button type="submit" variant="primary" size="sm" disabled={submitting}>
				<i class="bi bi-send me-1"></i>
				{submitting ? getMessage('ticket_comment_submitting') : getMessage('ticket_comment_submit')}
			</Button>
		</div>
	</div>
</form>
