<script lang="ts">
    import { onMount, onDestroy } from 'svelte';
    import { fade, scale } from 'svelte/transition';
    import { modalStore } from '$lib/stores/modal';
    import type { ModalOptions } from '$lib/stores/modal';

    let { options }: { options: ModalOptions } = $props();

    let modalElement: HTMLDivElement;
    let previousFocus: HTMLElement | null = null;

    // Modal size classes
    const sizeClasses: Record<string, string> = {
        sm: 'modal-sm',
        md: '',
        lg: 'modal-lg',
        xl: 'modal-xl'
    };

    const modalSizeClass = $derived(sizeClasses[options.size || 'md']);

    onMount(() => {
        // Store the currently focused element to restore focus when modal closes
        previousFocus = document.activeElement as HTMLElement;

        // Add body class to prevent scrolling
        document.body.classList.add('modal-open');

        // Focus the modal
        modalElement?.focus();

        // Add keyboard event listener
        window.addEventListener('keydown', handleKeydown);
    });

    onDestroy(() => {
        // Remove body class
        document.body.classList.remove('modal-open');

        // Restore focus to the previously focused element
        if (previousFocus && typeof previousFocus.focus === 'function') {
            previousFocus.focus();
        }

        // Remove keyboard event listener
        window.removeEventListener('keydown', handleKeydown);
    });

    function handleKeydown(e: KeyboardEvent) {
        if (e.key === 'Escape' && options.closeOnEscape !== false) {
            handleClose();
        }
    }

    function handleBackdropClick(e: MouseEvent) {
        if (e.target === e.currentTarget && options.closeOnBackdrop === true) {
            handleClose();
        }
    }

    function handleClose() {
        modalStore.close();
    }
</script>

<!-- Modal backdrop -->
<div
    class="modal-backdrop"
    transition:fade={{ duration: 150 }}
    onclick={handleBackdropClick}
    role="presentation"
>
    <!-- Modal dialog -->
    <div
        bind:this={modalElement}
        class="modal-dialog {modalSizeClass}"
        transition:scale={{ duration: 200, start: 0.95 }}
        role="dialog"
        aria-modal="true"
        tabindex="-1"
    >
        <div class="modal-content">
            <!-- Render the component passed in options -->
            <svelte:component this={options.component} {...(options.props || {})} onClose={handleClose} />
        </div>
    </div>
</div>

<style>
    .modal-backdrop {
        position: fixed;
        top: 0;
        left: 0;
        width: 100%;
        height: 100%;
        background-color: rgba(0, 0, 0, 0.5);
        display: flex;
        align-items: center;
        justify-content: center;
        z-index: 1050;
        padding: 1rem;
    }

    .modal-dialog {
        background: white;
        border-radius: 0.375rem;
        box-shadow: 0 0.5rem 1rem rgba(0, 0, 0, 0.15);
        max-width: 500px;
        width: 100%;
        max-height: 90vh;
        outline: none;
    }

    .modal-dialog.modal-sm {
        max-width: 20em;
    }

    .modal-dialog.modal-lg {
        max-width: 50em;
        max-height: 90vh;
    }

    .modal-dialog.modal-xl {
        max-width: 71.25em;
        max-height: 90vh;
    }

    .modal-content {
        display: flex;
        flex-direction: column;
    }

    /* Ensure modal is above everything */
    :global(body.modal-open) {
        overflow: hidden;
    }

    /* Focus styles */
    .modal-dialog:focus {
        border-color: var(--ironpack-border);
		outline: 0;
		box-shadow: 0 0 0 0.25rem 	rgba(var(--ironpack-border-rgb), 0.2);
        /* outline: 4px solid rgba(var(--bs-secondary-rgb), 0.2); */
        /* outline: 2px solid #0d6efd; */
        /* outline-offset: 2px; */
    }
</style>
