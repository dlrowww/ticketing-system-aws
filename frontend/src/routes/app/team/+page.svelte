<script lang="ts">
	import type { TicketQuery } from '$lib/types/tickets';
	import TicketsListPage from '$lib/components/tickets/TicketsListPage.svelte';

	let {
		data
	}: {
		data: { initial: any; query: TicketQuery; error?: string; categoryMissing?: boolean };
	} = $props();
</script>

<TicketsListPage
	titleKey="nav_team_tickets"
	noticeKey={data.categoryMissing ? 'tickets_category_required_for_view' : undefined}
	hideTable={!!data.categoryMissing}
	{data}
/>
