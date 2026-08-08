import { writable } from 'svelte/store';
import type { Component } from 'svelte';

export interface ModalOptions {
	/**
	 * Component to render inside the modal
	 */
	component: Component<any>;
	/**
	 * Props to pass to the modal component
	 */
	props?: Record<string, any>;
	/**
	 * Modal size: 'sm' | 'md' | 'lg' | 'xl'
	 */
	size?: 'sm' | 'md' | 'lg' | 'xl';
	/**
	 * Whether clicking outside the modal closes it (default: false)
	 */
	closeOnBackdrop?: boolean;
	/**
	 * Whether pressing Escape closes the modal (default: true)
	 */
	closeOnEscape?: boolean;
	/**
	 * Callback when modal is closed
	 */
	onClose?: () => void;
}

interface ModalState {
	isOpen: boolean;
	options: ModalOptions | null;
}

function createModalStore() {
	const { subscribe, set, update } = writable<ModalState>({
		isOpen: false,
		options: null
	});

	return {
		subscribe,
		/**
		 * Open a modal with the specified component and options
		 */
		open: (options: ModalOptions) => {
			update(() => ({
				isOpen: true,
				options: {
					size: 'md',
					closeOnBackdrop: false,
					closeOnEscape: true,
					...options
				}
			}));
		},
		/**
		 * Close the current modal
		 */
		close: () => {
			update((state) => {
				// Call onClose callback if provided
				if (state.options?.onClose) {
					state.options.onClose();
				}

				return {
					isOpen: false,
					options: null
				};
			});
		},
		/**
		 * Check if a modal is currently open
		 */
		isOpen: () => {
			let currentState: ModalState = { isOpen: false, options: null };
			const unsubscribe = subscribe((state) => {
				currentState = state;
			});
			unsubscribe();
			return currentState.isOpen;
		}
	};
}

export const modalStore = createModalStore();
