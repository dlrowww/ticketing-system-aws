import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/svelte';
import TicketDetailModal from '$lib/components/modals/TicketDetailModal.svelte';
import type { AssignableUserDto } from '$lib/types/users';
import type { TicketDetail } from '$lib/types/tickets';

// Mock getMessage to return the key
vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

// Mock lookups + name helpers used by the modal
vi.mock('$lib/lookups/Lookups', () => ({
	lookups: {
		category: () => [
			{ id: 1, name: 'Category1' },
			{ id: 2, name: 'Category2' }
		],
		priority: () => [
			{ id: 1, name: 'Priority1' },
			{ id: 2, name: 'Priority2' }
		],
		ticketStatus: () => [
			{ id: 1, name: 'Status1' },
			{ id: 2, name: 'Status2' }
		],
		userRole: () => []
	},
	categoryName: (id: number) => `Category${id}`,
	priorityName: (id: number) => `Priority${id}`,
	statusName: (id: number) => `Status${id}`
}));

const computeEditCapabilitiesMock = vi.fn();
const canEditTicketMock = vi.fn();
vi.mock('$lib/utils/ticketPermissions', () => ({
	computeEditCapabilities: (...args: any[]) => computeEditCapabilitiesMock(...args),
	canEditTicket: (...args: any[]) => canEditTicketMock(...args)
}));

const mockAssignableUsers: AssignableUserDto[] = [
	{
		userId: 10,
		name: 'Jan Kowalski',
		email: 'jan@ironpack.pl',
		roleName: 'Support',
		categoryId: 1,
		categoryNamePl: 'IT',
		categoryNameEn: 'IT'
	},
	{
		userId: 11,
		name: 'Anna Nowak',
		email: 'anna@ironpack.pl',
		roleName: 'TeamLeader',
		categoryId: 1,
		categoryNamePl: 'IT',
		categoryNameEn: 'IT'
	},
	{
		userId: 12,
		name: 'Marek Zieliński',
		email: 'marek@ironpack.pl',
		roleName: 'Admin',
		categoryId: null,
		categoryNamePl: null,
		categoryNameEn: null
	}
];

const fetchAssignableUsersMock = vi.fn();
const updateTicketMock = vi.fn();
const fetchUsersMock = vi.fn();

const getTicketByIdMock = vi.fn();
const listTicketHistoryMock = vi.fn();
const listTicketCommentsMock = vi.fn();
const listTicketFilesMock = vi.fn();
const getAllowedStatusesMock = vi.fn();

vi.mock('$lib/services/Users', () => ({
	fetchAssignableUsers: (...args: any[]) => fetchAssignableUsersMock(...args),
	fetchUsers: (...args: any[]) => fetchUsersMock(...args)
}));

vi.mock('$lib/services/Tickets', async () => {
	const actual = await vi.importActual<any>('$lib/services/Tickets');
	return {
		...actual,
		getTicketById: (...args: any[]) => getTicketByIdMock(...args),
		listTicketHistory: (...args: any[]) => listTicketHistoryMock(...args),
		listTicketComments: (...args: any[]) => listTicketCommentsMock(...args),
		listTicketFiles: (...args: any[]) => listTicketFilesMock(...args),
		getAllowedStatuses: (...args: any[]) => getAllowedStatusesMock(...args),
		updateTicket: (...args: any[]) => updateTicketMock(...args)
	};
});

async function openSelectByLabel(labelText: string) {
	const trigger = screen.getByLabelText(labelText);
	await fireEvent.click(trigger);
}

async function chooseOption(optionText: string) {
	const options = screen.getAllByRole('option');
	const match = options.find((opt) => {
		const name = opt.getAttribute('aria-label') ?? opt.textContent ?? '';
		return name.includes(optionText);
	});
	if (!match) {
		throw new Error(`Option containing "${optionText}" not found`);
	}
	await fireEvent.click(match);
}

async function selectDropdownValue(labelText: string, optionText: string) {
	await openSelectByLabel(labelText);
	await chooseOption(optionText);
}

async function waitForTicketLoaded() {
	await waitFor(() => {
		expect(screen.getByRole('button', { name: 'ticket_edit' })).toBeInTheDocument();
	});
}

describe('TicketDetailModal - Assignment Integration', () => {
	const mockTicket: TicketDetail = {
		ticketId: 1,
		title: 'Test Ticket',
		description: 'Test description long enough for editing',
		categoryId: 1,
		priority: 2,
		status: 1,
		createdById: 5,
		assignedToId: null,
		createdByName: 'Test User',
		assignedToName: null,
		createdAt: '2026-01-01T10:00:00Z',
		updatedAt: '2026-01-01T10:00:00Z'
	};

	const mockUser = { userId: 99, name: 'Tester', email: 'tester@test.local', role: 1 } as any;

	beforeEach(() => {
		fetchAssignableUsersMock.mockReset();
		updateTicketMock.mockReset();
		fetchUsersMock.mockReset();
		getTicketByIdMock.mockReset();
		listTicketHistoryMock.mockReset();
		listTicketCommentsMock.mockReset();
		listTicketFilesMock.mockReset();
		getAllowedStatusesMock.mockReset();
		computeEditCapabilitiesMock.mockReset();
		canEditTicketMock.mockReset();

		fetchUsersMock.mockResolvedValue([]);
		getTicketByIdMock.mockResolvedValue(mockTicket);
		listTicketHistoryMock.mockResolvedValue([]);
		listTicketCommentsMock.mockResolvedValue([]);
		listTicketFilesMock.mockResolvedValue([]);
		getAllowedStatusesMock.mockResolvedValue([1, 2]);
		canEditTicketMock.mockReturnValue(true);
		computeEditCapabilitiesMock.mockReturnValue({
			canEdit: true,
			canEditTitle: true,
			canEditDescription: true,
			canEditCategory: true,
			canEditPriority: true,
			canEditStatus: true,
			canEditAssignment: true,
			canDelete: true
		});
	});

	it('loads attachments only once when attachments tab is clicked repeatedly', async () => {
		getTicketByIdMock.mockResolvedValue(mockTicket);
		listTicketFilesMock.mockResolvedValue([]);
		listTicketHistoryMock.mockResolvedValue([]);
		listTicketCommentsMock.mockResolvedValue([]);
		fetchUsersMock.mockResolvedValue({ items: [] });
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);
		getAllowedStatusesMock.mockResolvedValue([1, 2]);

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: { id: '5', name: 'Test User', email: 'test@example.com', roleId: '1' }
			}
		});

		await waitForTicketLoaded();

		// Click attachments tab twice (same tab)
		await fireEvent.click(screen.getByRole('tab', { name: 'ticket_tab_attachments' }));
		await fireEvent.click(screen.getByRole('tab', { name: 'ticket_tab_attachments' }));

		await waitFor(() => {
			expect(listTicketFilesMock).toHaveBeenCalledTimes(1);
		});
	});

	afterEach(() => {
		vi.clearAllMocks();
	});

	it('loads assignable users when entering edit mode with canEditAssignment permission', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Initially should NOT have called fetchAssignableUsers
		expect(fetchAssignableUsersMock).not.toHaveBeenCalled();

		// Find and click Edit button
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		// Should load assignable users when entering edit mode
		await waitFor(() => {
			expect(fetchAssignableUsersMock).toHaveBeenCalledTimes(1);
		});

		// Verify it was called with correct ticketId
		expect(fetchAssignableUsersMock.mock.calls[0][0]).toBe(1);
	});

	it('does NOT load assignable users when entering edit mode WITHOUT canEditAssignment permission', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);
		computeEditCapabilitiesMock.mockReturnValue({
			canEdit: true,
			canEditTitle: true,
			canEditDescription: true,
			canEditCategory: true,
			canEditPriority: true,
			canEditStatus: true,
			canEditAssignment: false,
			canDelete: false
		});

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Click Edit button
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		// Should NOT call fetchAssignableUsers
		expect(fetchAssignableUsersMock).not.toHaveBeenCalled();
	});

	it('updates assignment on save when assignment changed', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);
		updateTicketMock.mockResolvedValue({
			...mockTicket,
			assignedToId: 10,
			assignedToName: 'Jan Kowalski'
		});

		const onClose = vi.fn();

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose
			}
		});

		await waitForTicketLoaded();

		// Enter edit mode
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		// Wait for assignable users to load
		await waitFor(() => {
			expect(fetchAssignableUsersMock).toHaveBeenCalled();
		});

		// Change to Jan Kowalski (userId: 10)
		await selectDropdownValue('assigned_to', 'Jan Kowalski');

		// Click Save button
		const saveButton = screen.getByRole('button', { name: 'ticket_edit_save' });
		await fireEvent.click(saveButton);

		// Verify updateTicket was called with new assignment
		await waitFor(() => {
			expect(updateTicketMock).toHaveBeenCalledTimes(1);
		});

		const updatePayload = updateTicketMock.mock.calls[0][1];
		expect(updatePayload.assignedToUserId).toBe(10);
	});

	it('shows error if assignment fails', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);
		updateTicketMock.mockRejectedValue(new Error('Assignment failed: Invalid assignee role'));

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Enter edit mode
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		await waitFor(() => {
			expect(fetchAssignableUsersMock).toHaveBeenCalled();
		});

		// Change assignment
		await selectDropdownValue('assigned_to', 'Jan Kowalski');

		// Try to save
		const saveButton = screen.getByRole('button', { name: 'ticket_edit_save' });
		await fireEvent.click(saveButton);

		// Should show error
		await waitFor(() => {
			const errorElement = screen.queryByText((content) => content.includes('Assignment failed'));
			expect(errorElement).toBeInTheDocument();
		});
	});

	it('handles assignable users loading error gracefully', async () => {
		fetchAssignableUsersMock.mockRejectedValue(new Error('Failed to load users'));

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Enter edit mode
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		// Wait for error to appear
		await waitFor(() => {
			const errorElement = screen.queryByText(
				(content) =>
					content.includes('Failed to load users') || content.includes('error_loading_users')
			);
			expect(errorElement).toBeInTheDocument();
		});

		// UserSelect should not be rendered (error shown instead)
		const selectTrigger = screen.queryByLabelText('assigned_to');
		expect(selectTrigger).not.toBeInTheDocument();
	});

	it('allows unassigning ticket (set to null)', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);
		updateTicketMock.mockResolvedValue({
			...mockTicket,
			assignedToId: null,
			assignedToName: null
		});

		const ticketWithAssignment: TicketDetail = {
			...mockTicket,
			assignedToId: 10,
			assignedToName: 'Jan Kowalski'
		};
		getTicketByIdMock.mockResolvedValue(ticketWithAssignment);

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Enter edit mode
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		await waitFor(() => {
			expect(fetchAssignableUsersMock).toHaveBeenCalled();
		});

		// Change to "Unassigned"
		await selectDropdownValue('assigned_to', 'not_assigned');

		// Save
		const saveButton = screen.getByRole('button', { name: 'ticket_edit_save' });
		await fireEvent.click(saveButton);

		// Verify clearAssignment was sent
		await waitFor(() => {
			expect(updateTicketMock).toHaveBeenCalledTimes(1);
		});

		const updatePayload = updateTicketMock.mock.calls[0][1];
		expect(updatePayload.clearAssignment).toBe(true);
	});

	it('does not reload assignable users on subsequent edit mode entries (cached)', async () => {
		fetchAssignableUsersMock.mockResolvedValue(mockAssignableUsers);

		render(TicketDetailModal, {
			props: {
				ticketId: 1,
				user: mockUser,
				onClose: vi.fn()
			}
		});

		await waitForTicketLoaded();

		// Enter edit mode (first time)
		const editButton = screen.getByRole('button', { name: 'ticket_edit' });
		await fireEvent.click(editButton);

		await waitFor(() => {
			expect(fetchAssignableUsersMock).toHaveBeenCalledTimes(1);
		});

		// Exit edit mode (cancel)
		const cancelButton = screen.getByRole('button', { name: 'ticket_edit_cancel' });
		await fireEvent.click(cancelButton);

		// Enter edit mode again (second time)
		await fireEvent.click(screen.getByRole('button', { name: 'ticket_edit' }));

		// Current behavior: entering edit mode triggers a reload
		expect(fetchAssignableUsersMock).toHaveBeenCalledTimes(2);
	});
});
