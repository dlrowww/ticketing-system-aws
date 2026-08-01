<script lang="ts">
	import { getMessage } from '$lib/i18n';
	import { formatDateTimeForDetails } from '$lib/utils/dateTime';
	import type { TicketHistoryDto } from '$lib/types/tickets';
	import { lookups } from '$lib/lookups/Lookups';
	import { statusName, priorityName } from '$lib/lookups/Lookups';
	import { getCategoryName } from '$lib/stores/categories';

	let {
		entries,
		loading = false,
		error = null
	}: {
		entries: TicketHistoryDto[];
		loading?: boolean;
		error?: string | null;
	} = $props();

	function labelForChangeType(changeType: string): string {
		const key = `ticket_history_change_${changeType}`;
		const label = getMessage(key);
		return label === key ? changeType : label;
	}

	function iconForChangeType(changeType: string): string {
		switch (changeType) {
			case 'TicketCreated':
				return 'bi-plus-circle';
			case 'StatusChanged':
				return 'bi-arrow-repeat';
			case 'AssignmentChanged':
				return 'bi-person-check';
			case 'PriorityChanged':
				return 'bi-exclamation-circle';
			case 'CategoryChanged':
				return 'bi-tags';
			case 'TitleChanged':
				return 'bi-type';
			case 'DescriptionChanged':
				return 'bi-card-text';
			default:
				return 'bi-clock-history';
		}
	}

	function formatDateTime(iso: string): string {
		return formatDateTimeForDetails(iso);
	}

	function formatHistoryValue(value: string | null, changeType: string, displayValue?: string | null): string {
		if (!value) return '';

		// For ticket creation, return as-is (already formatted)
		if (changeType === 'TicketCreated') {
			return value;
		}

		// Try to parse as number for enum lookups
		const numValue = parseInt(value, 10);

		switch (changeType) {
			case 'StatusChanged':
				if (!isNaN(numValue)) {
					const statusKey = statusName(numValue);
					return statusKey ? getMessage(statusKey) : value;
				}
				return value;

			case 'PriorityChanged':
				if (!isNaN(numValue)) {
					const priorityKey = priorityName(numValue);
					return priorityKey ? getMessage(priorityKey) : value;
				}
				return value;

			case 'CategoryChanged':
				if (!isNaN(numValue)) {
					return getCategoryName(numValue); // Locale-aware, DB-driven			}
				}
				return value;
			case 'AssignmentChanged':
				if (displayValue) return displayValue;
				if (!isNaN(numValue)) return `User #${value}`;
				return value;

			default:
				return value;
		}
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
{:else if entries.length === 0}
	<div class="alert alert-light" role="status">
		<i class="bi bi-inbox me-2"></i>
		{getMessage('ticket_history_empty')}
	</div>
{:else}
	<ul class="list-group">
		{#each entries as h (h.historyId)}
			<li class="list-group-item">
				<div class="d-flex align-items-start justify-content-between gap-3">
					<div class="d-flex gap-2">
						<i class={"bi " + iconForChangeType(h.changeType) + " text-secondary"}></i>
						<div>
							<div class="fw-semibold">{labelForChangeType(h.changeType)}</div>
							<div class="text-muted small">
								{h.changedByName} · {formatDateTime(h.changedAt)}
							</div>
							{#if h.oldValue || h.newValue}
								<div class="mt-2 small">
									{#if h.oldValue && h.newValue}
										{console.log('History with both values: ', h)}
										{#if h.changeType.toLowerCase().includes('change') && (h.oldValue === '0')}
											<span>{getMessage('not_available')}<span class="mx-1">&rarr;</span> {formatHistoryValue(h.newValue, h.changeType, h.newValueDisplay)}</span>
										{:else}
											<span>{formatHistoryValue(h.oldValue, h.changeType, h.oldValueDisplay)} <span class="mx-1">&rarr;</span> {formatHistoryValue(h.newValue, h.changeType, h.newValueDisplay)}</span>
										{/if}
									{:else if h.newValue}
										{#if h.changeType.toLowerCase().includes('change') && (!h.oldValue || h.oldValue === '0')}
											<span>{getMessage('not_available')}<span class="mx-1">&rarr;</span> {formatHistoryValue(h.newValue, h.changeType, h.newValueDisplay)}</span>
										{:else}
											<span>{formatHistoryValue(h.newValue, h.changeType, h.newValueDisplay)}</span>
										{/if}
									{:else if h.oldValue}
										<span>{formatHistoryValue(h.oldValue, h.changeType, h.oldValueDisplay)}</span>
									{/if}
								</div>
							{/if}
						</div>
					</div>
				</div>
			</li>
		{/each}
	</ul>
{/if}
