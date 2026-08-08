<script lang="ts">
	import type { TicketQuery } from '$lib/types/tickets';

	import TicketsTable from '$lib/components/tables/TicketsTable.svelte';
	import { exportTicketsCsv } from '$lib/services/Tickets';
	import { getMessage } from '$lib/i18n';
	import { modalStore } from '$lib/stores/modal';
	import TicketDetailModal from '$lib/components/modals/TicketDetailModal.svelte';

	let { data }: { data: { initial: any; query: TicketQuery; error?: string; user: any } } =
		$props();

	function onRow(payload: { action: string; id: number }) {
		const { action, id } = payload;
		if (action === 'view') {
			modalStore.open({
				component: TicketDetailModal,
				props: {
					ticketId: id,
					user: data.user,
					onTicketUpdated: () => {
						// Refresh the ticket list when ticket is updated
						window.dispatchEvent(new CustomEvent('tickets:refresh'));
					}
				},
				size: 'xl'
			});
		}
		// else: handle other actions (assign, close, etc.)
	}

	function onBulk(payload: { action: string; ids: number[] }) {
		const { action, ids } = payload;
		console.log('bulk:', action, ids);
		// Note: Bulk operations (bulk status change, reassignment) are a future enhancement
		// See PROJECT_PLAN.md - Nice-to-Have Features section
	}

	async function onExport() {
		try {
			// Use current query from the page data if present
			await exportTicketsCsv(data.query ?? {});
		} catch (e) {
			console.error('Export failed', e);
		}
	}
</script>

<section class="container py-3 d-flex flex-column gap-3 h-100" style="min-height: 0;">
	<h1 class="h4 mb-0">{getMessage('nav_all_tickets')}</h1>
	{#if data?.error}
		<div class="alert alert-warning">{data.error}</div>
	{/if}

	<div class="flex-grow-1 d-flex flex-column" style="min-height: 0;">
		<TicketsTable initial={data.initial} initialQuery={data.query} {onRow} {onBulk} {onExport} />
	</div>
</section>
