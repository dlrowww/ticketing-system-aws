import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import { toastStore } from '$lib/stores/toast';

// Mock getMessage to return the key so we can query predictable text.
// For keys with interpolation, return key_interpolated
vi.mock('$lib/i18n', () => ({
	getMessage: (key: string, values?: Record<string, any>) => {
		if (values && Object.keys(values).length > 0) {
			// Return key with values appended for interpolated messages
			return `${key}_${Object.values(values).join('_')}`;
		}
		return key;
	}
}));

// TicketFormModal derives options from lookups; provide deterministic lookups so Select has options.
vi.mock('$lib/lookups/Lookups', () => ({
	lookups: {
		category: () => [
			{ id: 1, name: '1' },
			{ id: 2, name: '2' }
		],
		priority: () => [
			{ id: 1, name: '1' },
			{ id: 2, name: '2' }
		],
		ticketStatus: () => [],
		userRole: () => []
	},
	toOptions: (items: Array<{ id: number; name: string }> = []) => items.map((i) => ({ value: i.id, labelKey: i.name, label: i.name }))
}));

// Use a per-file mock so we can assert calls precisely.
const createTicketMock = vi.fn();
vi.mock('$lib/services/Tickets', async () => {
	const actual = await vi.importActual<any>('$lib/services/Tickets');
	return {
		...actual,
		createTicket: (...args: any[]) => createTicketMock(...args)
	};
});

import TicketFormModal from '$lib/components/modals/TicketFormModal.svelte';

async function selectDropdownValue(fieldId: string, optionText: string) {
	const trigger = document.getElementById(fieldId) as HTMLElement;
	await fireEvent.click(trigger);
	await fireEvent.click(screen.getByRole('option', { name: optionText }));
}

describe('TicketFormModal', () => {
	beforeEach(() => {
		vi.useFakeTimers();
		createTicketMock.mockReset();
		vi.spyOn(toastStore, 'success');
		vi.spyOn(toastStore, 'error');
	});

	afterEach(() => {
		vi.restoreAllMocks();
		vi.useRealTimers();
	});

	it('submits valid form, shows success toast, closes, and dispatches refresh event', async () => {
		createTicketMock.mockResolvedValue({ ticketId: 123 });

		const onClose = vi.fn();
		const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

		render(TicketFormModal as any, { props: { onClose } });

		const titleInput = document.getElementById('ticket-title') as HTMLInputElement;
		const descInput = document.getElementById('ticket-description') as HTMLTextAreaElement;
		
		await fireEvent.input(titleInput, { target: { value: 'Valid ticket title' } });
		await fireEvent.input(descInput, {
			target: { value: 'This is a sufficiently long description for a ticket (>= 20 chars).' }
		});
		await selectDropdownValue('ticket-category', '1');
		await selectDropdownValue('ticket-priority', '2');

		await fireEvent.click(screen.getByText('ticket_create_submit'));

		expect(createTicketMock).toHaveBeenCalledTimes(1);
		const [formDataArg] = createTicketMock.mock.calls[0];
		expect(formDataArg).toBeInstanceOf(FormData);
		expect((formDataArg as FormData).get('Title')).toBe('Valid ticket title');
		expect((formDataArg as FormData).get('CategoryId')).toBe('1');
		expect((formDataArg as FormData).get('Priority')).toBe('2');

		// Let the modal's setTimeout(0) dispatch run.
		await vi.runAllTimersAsync();

		expect(toastStore.success).toHaveBeenCalledWith('ticket_create_success');
		expect(onClose).toHaveBeenCalledTimes(1);

		// Verify the app-level refresh event is dispatched.
		expect(dispatchSpy).toHaveBeenCalled();
		const refreshCall = dispatchSpy.mock.calls.find((c) => (c[0] as Event).type === 'tickets:refresh');
		expect(refreshCall).toBeTruthy();
	});

	it('does not submit when form is invalid (client-side validation)', async () => {
		createTicketMock.mockResolvedValue({ ticketId: 123 });

		render(TicketFormModal as any);

		// The form starts with empty fields - button should be disabled due to isFormValid check
		const submitButton = screen.getByText('ticket_create_submit').closest('button');
		expect(submitButton).toBeTruthy();
		expect(submitButton?.hasAttribute('disabled')).toBe(true);

		// createTicketMock should not be called since form is invalid
		expect(createTicketMock).not.toHaveBeenCalled();
	});

	it('shows backend field errors and does not close modal', async () => {
		createTicketMock.mockRejectedValue({
			fieldErrors: {
				Title: ['TICKET_TITLE_TOO_SHORT', 'TICKET_TITLE_TOO_LONG'],
				Description: ['TICKET_DESCRIPTION_TOO_SHORT', 'TICKET_DESCRIPTION_TOO_LONG']
			}
		});

		const onClose = vi.fn();
		render(TicketFormModal as any, { props: { onClose } });

		const titleInput = document.getElementById('ticket-title') as HTMLInputElement;
		const descInput = document.getElementById('ticket-description') as HTMLTextAreaElement;
		
		await fireEvent.input(titleInput, { target: { value: 'Valid ticket title' } });
		await fireEvent.input(descInput, {
			target: { value: 'This is a sufficiently long description for a ticket (>= 20 chars).' }
		});
		await selectDropdownValue('ticket-category', '1');
		await selectDropdownValue('ticket-priority', '2');

		await fireEvent.click(screen.getByText('ticket_create_submit'));

		expect(createTicketMock).toHaveBeenCalledTimes(1);
		expect(toastStore.error).toHaveBeenCalledWith('ticket_create_fix_errors');
		expect(onClose).not.toHaveBeenCalled();
		expect(screen.getByText('error_code_TICKET_TITLE_TOO_SHORT')).toBeTruthy();
		expect(screen.getByText('error_code_TICKET_TITLE_TOO_LONG')).toBeTruthy();
		expect(screen.getByText('error_code_TICKET_DESCRIPTION_TOO_SHORT')).toBeTruthy();
		expect(screen.getByText('error_code_TICKET_DESCRIPTION_TOO_LONG')).toBeTruthy();
	});
});
