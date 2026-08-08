<script lang="ts">
	import type { TicketStatus } from '$lib/types/enums';
	import { TicketStatus as TS } from '$lib/types/enums';
	import { statusName } from '$lib/lookups/Lookups';
	import { getStatusClassName } from '$lib/theme/statusPalette';
	import { getMessage } from '$lib/i18n';
	import Badge from '$lib/components/ui/Badge.svelte';

	// Svelte 5 props
	let { status }: { status: TicketStatus } = $props();

	// derive i18n key from centralized lookups (fallback safe)
	const labelKey = $derived(statusName(status) ?? 'ticket_status_unknown');
	// translate it (store auto-subscription works with $t)
	const label = $derived(getMessage(labelKey));
	const badgeClass = $derived(getStatusClassName(status));
</script>

<!-- Use generic Badge component with custom variant for IronPack styling -->
<Badge variant="custom" class={badgeClass} ariaLabel={label}>
	{label}
</Badge>

<style>
	/* Neutral / incoming */
	:global(.bg-status-new) {
		background: #f1f3f5;
		color: #495057;
	} /* gray */

	/* Active / backlog (not danger) */
	:global(.bg-status-open) {
		background: #e7f5ff;
		color: #1c7ed6;
	} /* blue */

	/* Work in progress */
	:global(.bg-status-inprocess) {
		background: #fff9db;
		color: #f59f00;
	} /* yellow */

	/* Done / successful */
	:global(.bg-status-resolved) {
		background: #ebfbee;
		color: #2f9e44;
	} /* green */

	/* Terminated / negative */
	:global(.bg-status-cancelled) {
		background: #ffe3e3;
		color: #e03131;
	} /* red */

	/* Waiting / paused (blocked) */
	:global(.bg-status-postponed) {
		background: #fff3bf;
		color: #f08c00;
	} /* amber */

	/* Returned / rework (quality signal), distinct from cancelled but still “attention” */
	:global(.bg-status-returned) {
		background: #f3f0ff;
		color: #6741d9;
	} /* violet */

	/* Fallback */
	:global(.bg-status-unknown) {
		background: #f1f3f5;
		color: #6c757d;
	} /* gray */
</style>
