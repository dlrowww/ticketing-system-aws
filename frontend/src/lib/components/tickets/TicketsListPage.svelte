<script lang="ts">
	import type { TicketQuery } from '$lib/types/tickets';

	import TicketsTable from '$lib/components/tables/TicketsTable.svelte';
	import { exportTicketsCsv } from '$lib/services/Tickets';
	import { getMessage } from '$lib/i18n';
	import { modalStore } from '$lib/stores/modal';
	import TicketDetailModal from '$lib/components/modals/TicketDetailModal.svelte';

	type Props = {
		titleKey: string;
		data: { initial: any; query: TicketQuery; error?: string; user?: any };
		noticeKey?: string;
		hideTable?: boolean;
	};

	let { titleKey, data, noticeKey, hideTable }: Props = $props();

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
	}

	function onBulk(payload: { action: string; ids: number[] }) {
		const { action, ids } = payload;
		console.log('bulk:', action, ids);
	}

	async function onExport() {
		try {
			await exportTicketsCsv(data.query ?? {});
		} catch (e) {
			console.error('Export failed', e);
		}
	}
</script>

<section class="container py-3 d-flex flex-column gap-3 h-100" style="min-height: 0;">
	<div class="d-flex flex-column gap-2">
		<h1 class="h4 mb-0">{getMessage(titleKey)}</h1>

		{#if data?.error}
			<div class="alert alert-warning mb-0">{data.error}</div>
		{/if}

		{#if noticeKey}
			<div class="alert alert-warning mb-0">{getMessage(noticeKey)}</div>
		{/if}
	</div>

	{#if !hideTable}
		<div class="flex-grow-1 d-flex flex-column" style="min-height: 0;">
			<TicketsTable initial={data.initial} initialQuery={data.query} {onRow} {onBulk} {onExport} />
		</div>
	{/if}
</section>
