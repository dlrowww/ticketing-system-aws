<script lang="ts">
	import type { Priority } from '$lib/types/enums';
	import { Priority as TP } from '$lib/types/enums';
	import { priorityName } from '$lib/lookups/Lookups';
	import { getMessage } from '$lib/i18n';
	import Badge from '$lib/components/ui/Badge.svelte';

	/**
	 * Priority Badge with IronPack colors and icons
	 * 
	 * @example
	 * <PriorityBadge priority={Priority.PriorityHigh} />
	 */

	// Svelte 5 props
	let { priority }: { priority: Priority } = $props();

	// Map priority to custom CSS class and icon
	function priorityClass(p: Priority) {
		switch (p) {
			case TP.Low:      return 'bg-priority-low';
			case TP.Medium:   return 'bg-priority-medium';
			case TP.High:     return 'bg-priority-high';
			case TP.Critical: return 'bg-priority-critical';
			default:          return 'bg-priority-unknown';
		}
	}

	function priorityIcon(p: Priority): string {
		switch (p) {
			case TP.Low:      return 'bi-arrow-down';
			case TP.Medium:   return 'bi-dash';
			case TP.High:     return 'bi-arrow-up';
			case TP.Critical: return 'bi-exclamation-triangle-fill';
			default:          return 'bi-question-circle';
		}
	}

	// derive i18n key from centralized lookups
	const labelKey = $derived(priorityName(priority) ?? 'ticket_priority_unknown');
	const label = $derived(getMessage(labelKey));
	const icon = $derived(priorityIcon(priority));
</script>

<!-- Use generic Badge component with custom variant -->
<Badge variant="custom" class={priorityClass(priority)} ariaLabel={label}>
	<i class="bi {icon}" style="margin-right: 0.35rem;"></i>
	{label}
</Badge>

<style>
	/* IronPack-toned priority palette */
	:global(.bg-priority-low) {
		background: #e8f5f1;
		color: #0d5c45;
	}

	:global(.bg-priority-medium) {
		background: #fff8e1;
		color: #a57800;
	}

	:global(.bg-priority-high) {
		background: #ffe8e1;
		color: #c43e00;
	}

	:global(.bg-priority-critical) {
		background: #fde2e2;
		color: #8f1e1e;
		font-weight: 600;
	}

	:global(.bg-priority-unknown) {
		background: #f0f0f0;
		color: #666;
	}

	/* Icon spacing */
	:global(.bg-priority-low i),
	:global(.bg-priority-medium i),
	:global(.bg-priority-high i),
	:global(.bg-priority-critical i),
	:global(.bg-priority-unknown i) {
		margin-right: 0.35rem;
	}
</style>
