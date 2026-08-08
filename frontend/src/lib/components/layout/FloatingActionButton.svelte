<script lang="ts">
	import { modalStore } from '$lib/stores/modal';
	import { getMessage } from '$lib/i18n';
	import TicketFormModal from '$lib/components/modals/TicketFormModal.svelte';

	function openCreateTicket() {
		modalStore.open({
			component: TicketFormModal,
			size: 'lg'
		});
	}
</script>

<button
	class="fab"
	onclick={openCreateTicket}
	aria-label={getMessage('fab_create_ticket')}
	title={getMessage('fab_create_ticket')}
>
	<i class="bi bi-plus-lg"></i>
</button>

<style>
	.fab {
		position: fixed;
		bottom: 5rem;
		right: 2rem;
		width: 56px;
		height: 56px;
		border: none;
		border-radius: 50%;
		background: linear-gradient(
			135deg,
			var(--ironpack-gradient-from) 0%,
			var(--ironpack-gradient-to) 100%
		);
		color: var(--ironpack-white);
		font-size: 1.5rem;
		box-shadow: 0 4px 12px rgba(var(--ironpack-red-rgb), 0.35);
		cursor: pointer;
		z-index: 1000;
		display: flex;
		align-items: center;
		justify-content: center;
		transition: all 0.3s ease;
		opacity: 0.55;
	}

	.fab:hover {
		background: linear-gradient(135deg, var(--ironpack-border) 0%, var(--ironpack-bg-dark) 100%);
		transform: scale(1.1);
		box-shadow: 0 6px 16px rgba(var(--ironpack-red-rgb), 0.45);
	}

	.fab:active {
		transform: scale(0.95);
	}

	.fab i {
		font-size: 1.5rem;
		line-height: 1;
		color: inherit;
	}
</style>
