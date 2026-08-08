<script lang="ts">
	import { onMount } from 'svelte';
	import { goto } from '$app/navigation';

	import TicketDetailModal from '$lib/components/modals/TicketDetailModal.svelte';
	import { modalStore } from '$lib/stores/modal';
	import { toastStore } from '$lib/stores/toast';
	import { getMessage } from '$lib/i18n';

	let { data }: { data: { ticketId: number; user?: any } } = $props();

	onMount(() => {
		modalStore.open({
			component: TicketDetailModal,
			props: {
				ticketId: data.ticketId,
				user: data.user,
				onTicketUpdated: () => {
					// Note: This won't have effect since we redirect away from this page
					// But included for consistency
					window.dispatchEvent(new CustomEvent('tickets:refresh'));
				}
			},
			size: 'xl'
		});

		toastStore.info(getMessage('ticket_detail_deprecated_message'));

		void goto('/app/tickets');
	});
</script>

<!-- This route is deprecated. It immediately opens a modal and redirects. -->
