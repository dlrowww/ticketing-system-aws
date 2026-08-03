import { describe, it, expect } from 'vitest';
import { computeEditCapabilities, canEditTicket } from '$lib/utils/ticketPermissions';
import { TicketStatus } from '$lib/types/enums';
import { UserRole } from '$lib/types/enums';
import type { TicketDetail } from '$lib/types/tickets';
import type { User } from '$lib/types/user';

// Helper to create minimal TicketDetail
function createTicket(overrides: Partial<TicketDetail> = {}): TicketDetail {
	return {
		ticketId: 1,
		title: 'Test Ticket',
		description: 'Test Description',
		categoryId: 1,
		priority: 2,
		status: TicketStatus.Open,
		createdById: 100,
		createdByName: 'Creator',
		createdAt: '2025-01-01T10:00:00Z',
		updatedAt: '2025-01-01T10:00:00Z',
		assignedToId: 200,
		assignedToName: 'Assignee',
		capabilities: undefined,
		...overrides
	};
}

// Helper to create minimal User
function createUser(
	userId: number,
	role: UserRole,
	categoryId: number | null = null
): User {
	return {
		id: String(userId),
		name: `User ${userId}`,
		email: `user${userId}@test.local`,
		roleId: String(role),
		categoryId: categoryId == null ? undefined : String(categoryId)
	};
}

describe('ticketPermissions', () => {
	describe('computeEditCapabilities - Admin Role', () => {
		const admin = createUser(1, UserRole.Admin);

		it('should grant all permissions for non-terminal status', () => {
			const ticket = createTicket({ status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, admin);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});

		it('should deny all permissions for Resolved status', () => {
			const ticket = createTicket({ status: TicketStatus.Resolved });
			const capabilities = computeEditCapabilities(ticket, admin);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should deny all permissions for Cancelled status', () => {
			const ticket = createTicket({ status: TicketStatus.Cancelled });
			const capabilities = computeEditCapabilities(ticket, admin);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should grant all permissions for New status', () => {
			const ticket = createTicket({ status: TicketStatus.New });
			const capabilities = computeEditCapabilities(ticket, admin);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});
	});

	describe('computeEditCapabilities - TeamLeader Role', () => {
		const teamLeader = createUser(2, UserRole.TeamLeader, 1); // Category 1 = IT

		it('should grant all permissions for ticket in same category', () => {
			const ticket = createTicket({ categoryId: 1, status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, teamLeader);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});

		it('should deny all permissions for ticket in different category', () => {
			const ticket = createTicket({ categoryId: 2, status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, teamLeader);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should deny all permissions for terminal status even in same category', () => {
			const ticket = createTicket({ categoryId: 1, status: TicketStatus.Resolved });
			const capabilities = computeEditCapabilities(ticket, teamLeader);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should deny all permissions if TeamLeader has no category', () => {
			const teamLeaderNoCategory = createUser(3, UserRole.TeamLeader, null);
			const ticket = createTicket({ categoryId: 1, status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, teamLeaderNoCategory);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});
	});

	describe('computeEditCapabilities - Support Role', () => {
		const support = createUser(3, UserRole.Support, 1); // Category 1 = IT

		it('should grant content/status permissions for assigned ticket in same category', () => {
			const ticket = createTicket({
				categoryId: 1,
				status: TicketStatus.InProcess,
				assignedToId: 3
			});
			const capabilities = computeEditCapabilities(ticket, support);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});

		it('should grant all permissions for unassigned ticket in same category', () => {
			const ticket = createTicket({
				categoryId: 1,
				status: TicketStatus.Open,
				assignedToId: null
			});
			const capabilities = computeEditCapabilities(ticket, support);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});

		it('should deny all permissions for ticket not assigned and outside category', () => {
			const ticket = createTicket({
				categoryId: 2,
				status: TicketStatus.InProcess,
				assignedToId: 999 // Different user
			});
			const capabilities = computeEditCapabilities(ticket, support);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should grant all permissions for assigned ticket in different category', () => {
			const ticket = createTicket({
				categoryId: 2, // Different category
				status: TicketStatus.InProcess,
				assignedToId: 3
			});
			const capabilities = computeEditCapabilities(ticket, support);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(true);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(true);
			expect(capabilities.canEditAssignment).toBe(true);
		});

		it('should deny all permissions for terminal status', () => {
			const ticket = createTicket({
				categoryId: 1,
				status: TicketStatus.Resolved,
				assignedToId: 3
			});
			const capabilities = computeEditCapabilities(ticket, support);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});
	});

	describe('computeEditCapabilities - Employee Role', () => {
		const employee = createUser(4, UserRole.Employee);

		it('should grant title/description permissions for own ticket in mutable status', () => {
			const ticket = createTicket({
				status: TicketStatus.New,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should grant permissions for Open status', () => {
			const ticket = createTicket({
				status: TicketStatus.Open,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
		});

		it('should grant permissions for Returned status', () => {
			const ticket = createTicket({
				status: TicketStatus.Returned,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
		});

		it('should grant permissions for InProgress status', () => {
			const ticket = createTicket({
				status: TicketStatus.InProcess,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
		});

		it('should grant permissions for Postponed status', () => {
			const ticket = createTicket({
				status: TicketStatus.Postponed,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(true);
			expect(capabilities.canEditDescription).toBe(true);
		});

		it('should deny permissions for terminal status', () => {
			const ticket = createTicket({
				status: TicketStatus.Resolved,
				createdById: 4
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
		});

		it('should deny permissions for ticket created by different user', () => {
			const ticket = createTicket({
				status: TicketStatus.New,
				createdById: 999 // Different user
			});
			const capabilities = computeEditCapabilities(ticket, employee);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});
	});

	describe('computeEditCapabilities - Edge Cases', () => {
		it('should return no permissions when user is null', () => {
			const ticket = createTicket({ status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, null);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should return no permissions when user is undefined', () => {
			const ticket = createTicket({ status: TicketStatus.Open });
			const capabilities = computeEditCapabilities(ticket, undefined);

			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(false);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(false);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(false);
		});

		it('should prefer backend capabilities over computed capabilities when provided', () => {
			const ticket = createTicket({
				status: TicketStatus.Open,
				capabilities: {
					canEditTitle: false, // Backend says no
					canEditDescription: true,
					canEditCategory: false,
					canEditPriority: true,
					canEditStatus: false,
					canEditAssignment: true
				}
			});
			const admin = createUser(1, UserRole.Admin);
			const capabilities = computeEditCapabilities(ticket, admin);

			// Should use backend capabilities exactly
			expect(capabilities.canEditTitle).toBe(false);
			expect(capabilities.canEditDescription).toBe(true);
			expect(capabilities.canEditCategory).toBe(false);
			expect(capabilities.canEditPriority).toBe(true);
			expect(capabilities.canEditStatus).toBe(false);
			expect(capabilities.canEditAssignment).toBe(true);
		});
	});

	describe('canEditTicket', () => {
		it('should return true if any permission is granted', () => {
			const ticket = createTicket({ status: TicketStatus.New, createdById: 4 });
			const employee = createUser(4, UserRole.Employee);

			expect(canEditTicket(ticket, employee)).toBe(true);
		});

		it('should return false if no permissions are granted', () => {
			const ticket = createTicket({ status: TicketStatus.Resolved });
			const employee = createUser(4, UserRole.Employee);

			expect(canEditTicket(ticket, employee)).toBe(false);
		});

		it('should return false when user is null', () => {
			const ticket = createTicket({ status: TicketStatus.Open });

			expect(canEditTicket(ticket, null)).toBe(false);
		});

		it('should return false when user is undefined', () => {
			const ticket = createTicket({ status: TicketStatus.Open });

			expect(canEditTicket(ticket, undefined)).toBe(false);
		});

		it('should return true for admin on non-terminal ticket', () => {
			const ticket = createTicket({ status: TicketStatus.Open });
			const admin = createUser(1, UserRole.Admin);

			expect(canEditTicket(ticket, admin)).toBe(true);
		});
	});
});
