import type { TicketStatus, Priority } from '$lib/types/enums';

export type DashboardStats = {
	totalTickets: number;
	openTickets: number;
	inProgressTickets: number;
	resolvedTickets: number;
	averageResolutionTimeHours: number;
};

export type TicketCountByCategory = {
	categoryId: number; // Database FK to Categories table
	count: number;
};

export type TicketCountByStatus = {
	status: TicketStatus;
	count: number;
};

export type TicketCountByPriority = {
	priority: Priority;
	count: number;
};

// DateOnly serialized by ASP.NET as YYYY-MM-DD
export type TicketTrendPoint = {
	date: string;
	count: number;
};
