import type { TicketDetail } from '$lib/types/tickets';
import { TicketStatus, UserRole } from '$lib/types/enums';
import type { User } from '$lib/types/user';

/**
 * Edit capabilities for a ticket.
 * Mirrors backend TicketEditCapabilitiesDto.
 */
export interface TicketEditCapabilities {
	canEditTitle: boolean;
	canEditDescription: boolean;
	canEditCategory: boolean;
	canEditPriority: boolean;
	canEditStatus: boolean;
	canEditAssignment: boolean;
}

/**
 * Client-side fallback to compute edit capabilities when backend doesn't provide them.
 * Should mirror backend TicketEditPolicy logic.
 * 
 * @param ticket - The ticket to check permissions for
 * @param user - The current authenticated user
 * @returns Edit capabilities object
 */
export function computeEditCapabilities(
	ticket: TicketDetail,
	user: User | null | undefined
): TicketEditCapabilities {
	// Fallback: client-side computation (mirrors backend logic)
	if (!user) {
		return noPermissions();
	}

	const role = user.roleId as UserRole;
	const userId = user.id;
	const isCreator = ticket.createdById === userId;
	const isAssignee = ticket.assignedToId === userId;
	const ticketCategory = ticket.categoryId;
	const userCategory = user.categoryId;
	const ticketStatus = ticket.status as TicketStatus;

	// Terminal states (Resolved, Cancelled) block ALL edits
	if (ticketStatus === TicketStatus.Resolved || ticketStatus === TicketStatus.Cancelled) {
		return noPermissions();
	}

	// Compute base permission
	let canEdit = false;

	if (role === UserRole.Admin) {
		canEdit = true;
	} else if (role === UserRole.TeamLeader) {
		// TeamLeader can edit if ticket is in their category
		canEdit = userCategory != null && userCategory === ticketCategory;
	} else if (role === UserRole.Support) {
		// Support can edit if assigned OR ticket in their category
		canEdit = isAssignee || (userCategory != null && userCategory === ticketCategory);
	} else if (role === UserRole.Employee) {
		// Employee can ONLY edit if they created the ticket AND it's in a mutable state
		const mutableStates = [
			TicketStatus.New,
			TicketStatus.Open,
			TicketStatus.InProcess,
			TicketStatus.Postponed,
			TicketStatus.Returned
		];
		canEdit = isCreator && mutableStates.includes(ticketStatus);
	}

	if (!canEdit) {
		return noPermissions();
	}

	// Field-level permissions
	const canEditTitleDesc = canEdit; // All roles if they have base permission
	const canEditRestricted = canEdit && [UserRole.Admin, UserRole.TeamLeader, UserRole.Support].includes(role);

	return {
		canEditTitle: canEditTitleDesc,
		canEditDescription: canEditTitleDesc,
		canEditCategory: canEditRestricted,
		canEditPriority: canEditRestricted,
		canEditStatus: canEditRestricted,
		canEditAssignment: canEditRestricted
	};
}

/**
 * Check if user has ANY edit permission for the ticket.
 */
export function canEditTicket(ticket: TicketDetail, user: User | null | undefined): boolean {
	const caps = computeEditCapabilities(ticket, user);
	return caps.canEditTitle || caps.canEditDescription || caps.canEditCategory ||
		caps.canEditPriority || caps.canEditStatus || caps.canEditAssignment;
}

/**
 * Helper: no permissions object
 */
function noPermissions(): TicketEditCapabilities {
	return {
		canEditTitle: false,
		canEditDescription: false,
		canEditCategory: false,
		canEditPriority: false,
		canEditStatus: false,
		canEditAssignment: false
	};
}
