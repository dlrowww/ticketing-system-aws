// Manual static enums (values defined in backend, rarely change)
// These are NOT auto-generated - backend enums are stable

export enum UserRole {
	Employee = 1,
	Support = 2,
	TeamLeader = 3,
	Admin = 4
}

export enum TicketStatus {
	New = 1,
	Open = 2,
	InProcess = 3,
	Resolved = 4,
	Cancelled = 5,
	Postponed = 6,
	Returned = 7
}

export enum Priority {
	Low = 1,
	Medium = 2,
	High = 3,
	Critical = 4
}

// i18n lookup keys (for getMessage() calls - UI translations only)
export const UserRoleKey: Record<number, string> = {
	[UserRole.Employee]: 'role_employee',
	[UserRole.Support]: 'role_support',
	[UserRole.TeamLeader]: 'role_team_leader',
	[UserRole.Admin]: 'role_admin'
};

export const TicketStatusKey: Record<number, string> = {
	[TicketStatus.New]: 'ticket_status_new',
	[TicketStatus.Open]: 'ticket_status_open',
	[TicketStatus.InProcess]: 'ticket_status_in_process',
	[TicketStatus.Resolved]: 'ticket_status_resolved',
	[TicketStatus.Cancelled]: 'ticket_status_cancelled',
	[TicketStatus.Postponed]: 'ticket_status_postponed',
	[TicketStatus.Returned]: 'ticket_status_returned'
};

export const PriorityKey: Record<number, string> = {
	[Priority.Low]: 'priority_low',
	[Priority.Medium]: 'priority_medium',
	[Priority.High]: 'priority_high',
	[Priority.Critical]: 'priority_critical'
};

// Type aliases for backward compatibility
export type UserRoleValue = UserRole;
export type TicketStatusValue = TicketStatus;
export type PriorityValue = Priority;

// NOTE: Categories are DB-driven and fetched from /api/categories
// Use the `categories` store from $lib/stores/categories instead
