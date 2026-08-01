import { describe, it, expect, vi } from 'vitest';
import { render, fireEvent, screen } from '@testing-library/svelte';
import { get } from 'svelte/store';
import FloatingActionButton from '$lib/components/layout/FloatingActionButton.svelte';
import TicketFormModal from '$lib/components/modals/TicketFormModal.svelte';
import { modalStore } from '$lib/stores/modal';

vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

describe('FloatingActionButton', () => {
	it('opens TicketFormModal on click', async () => {
		modalStore.close();
		render(FloatingActionButton as any);

		const button = screen.getByRole('button');
		await fireEvent.click(button);

		const state = get(modalStore);
		expect(state.isOpen).toBe(true);
		expect(state.options?.component).toBe(TicketFormModal);
	});
});
