<script lang="ts">
	import { modalStore } from '$lib/stores/modal';
	import Modal from './Modal.svelte';

	// Subscribe to the modal store state
	const modalState = $derived($modalStore);
</script>

{#if modalState.isOpen && modalState.options}
	<Modal options={modalState.options} />
{/if}
