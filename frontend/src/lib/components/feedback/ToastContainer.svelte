<script lang="ts">
    import { toastStore } from '$lib/stores/toast';
    import Toast from './Toast.svelte';

    const toasts = $derived($toastStore.toasts);
</script>

<div class="toast-container position-fixed top-0 start-50 translate-middle-x p-3" style="z-index: 9999;">
    {#each toasts as toast (toast.id)}
        <Toast {toast} />
    {/each}
</div>

<style>
    .toast-container {
        pointer-events: none;
    }

    .toast-container :global(.toast-item) {
        pointer-events: auto;
    }
</style>
