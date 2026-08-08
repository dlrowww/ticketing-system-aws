import { writable } from 'svelte/store';

export type ToastType = 'success' | 'error' | 'warning' | 'info';

export interface Toast {
	id: string;
	type: ToastType;
	message: string;
	duration?: number; // milliseconds, 0 = no auto-dismiss
}

interface ToastStore {
	toasts: Toast[];
}

function createToastStore() {
	const { subscribe, update } = writable<ToastStore>({ toasts: [] });

	return {
		subscribe,
		/**
		 * Add a toast notification
		 * @param type - Type of toast (success, error, warning, info)
		 * @param message - Message to display
		 * @param duration - Auto-dismiss duration in ms (default: 3000, 0 = no auto-dismiss)
		 */
		add: (type: ToastType, message: string, duration: number = 3000) => {
			const id = `toast-${Date.now()}-${Math.random()}`;
			const toast: Toast = { id, type, message, duration };

			update((state) => ({
				toasts: [...state.toasts, toast]
			}));

			// Auto-dismiss if duration > 0
			if (duration > 0) {
				setTimeout(() => {
					toastStore.remove(id);
				}, duration);
			}

			return id;
		},
		/**
		 * Remove a toast by ID
		 */
		remove: (id: string) => {
			update((state) => ({
				toasts: state.toasts.filter((t) => t.id !== id)
			}));
		},
		/**
		 * Clear all toasts
		 */
		clear: () => {
			update(() => ({ toasts: [] }));
		},
		/**
		 * Convenience methods for common toast types
		 */
		success: (message: string, duration?: number) => toastStore.add('success', message, duration),
		error: (message: string, duration?: number) => toastStore.add('error', message, duration),
		warning: (message: string, duration?: number) => toastStore.add('warning', message, duration),
		info: (message: string, duration?: number) => toastStore.add('info', message, duration)
	};
}

export const toastStore = createToastStore();
