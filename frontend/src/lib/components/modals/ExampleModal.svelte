<script lang="ts">
    /**
     * Example Modal Component
     * 
     * This demonstrates how to create a modal component that works with the modal store.
     * The modal store will pass an `onClose` prop to this component.
     */
    
    import { getMessage } from '$lib/i18n';
    
    let { title, message, onClose }: { 
        title?: string; 
        message?: string; 
        onClose?: () => void 
    } = $props();

    // Use i18n for default values
    const modalTitle = $derived(title ?? getMessage('example_modal_title'));
    const modalMessage = $derived(message ?? getMessage('example_modal_message'));

    function handleConfirm() {
        console.log(getMessage('modal_confirmed'));
        onClose?.();
    }

    function handleCancel() {
        console.log(getMessage('modal_cancelled'));
        onClose?.();
    }
</script>

<div class="modal-header">
    <h5 class="modal-title">{modalTitle}</h5>
    <button type="button" class="btn-close" aria-label={getMessage('close')} onclick={onClose}></button>
</div>

<div class="modal-body">
    <p>{modalMessage}</p>
</div>

<div class="modal-footer">
    <button type="button" class="btn btn-secondary" onclick={handleCancel}>{getMessage('cancel')}</button>
    <button type="button" class="btn btn-primary" onclick={handleConfirm}>{getMessage('confirm')}</button>
</div>

<style>
    .modal-header,
    .modal-body,
    .modal-footer {
        padding: 1rem;
    }

    .modal-header {
        border-bottom: 1px solid #dee2e6;
    }

    .modal-footer {
        border-top: 1px solid #dee2e6;
        display: flex;
        gap: 0.5rem;
        justify-content: flex-end;
    }

    .modal-title {
        margin: 0;
        font-size: 1.25rem;
        font-weight: 500;
    }
</style>
