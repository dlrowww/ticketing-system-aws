<script lang="ts">
	import type { TicketQuery } from '$lib/types/tickets';
	import TicketsListPage from '$lib/components/tickets/TicketsListPage.svelte';

	let { data }: { data: { initial: any; query: TicketQuery; error?: string } } = $props();
</script>

<TicketsListPage titleKey="nav_my_workload" {data} />
