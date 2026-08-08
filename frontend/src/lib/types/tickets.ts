// Manual static enums (not auto-generated)
import type { TicketStatus, Priority, UserRole } from '$lib/types/enums';

export {
	TicketStatus,
	TicketStatusKey,
	Priority,
	PriorityKey,
	UserRole,
	UserRoleKey
} from '$lib/types/enums';
// NOTE: Categories are DB-driven - use getCategoryName() from $lib/stores/categories
/** ISO 8601 date strings (e.g., "2025-08-15T10:00:00Z"). */
export type ISODate = string;

/* ===== Models and paging - start ================ **/

export interface TicketListItem {
	ticketId: number;
	title: string;
	categoryId: number; // Database FK to Categories table
	priority: Priority;
	status: TicketStatus;
	createdAt: ISODate;
	updatedAt?: ISODate;
	createdByName?: string;
	assignedToName?: string | null;
}

export interface PagedResult<T> {
	items: T[];
	total: number;
	page: number;
	size: number; // note: was pageSize before
	totalPages: number;
	hasNext: boolean;
	hasPrevious: boolean;
	sort?: string | null;
}

/* ===== Models and paging - end ================ **/

/* ===== Query split (paging/sort/filter) - start ========= **/

/** Allow 'All' sentinel in filters for UI convenience. */
export type All<T> = T | 'All';

export interface Paging {
	/** 1-based page index. */
	page?: number;
	pageSize?: number;
}

export type TicketSortableField =
	| 'ticketId'
	| 'title'
	| 'category'
	| 'priority'
	| 'status'
	| 'createdAt'
	| 'updatedAt'
	| 'createdByName'
	| 'assignedToName';

export interface Sorting<F extends string = TicketSortableField> {
	sortBy?: F;
	sortDir?: 'asc' | 'desc';
}

export interface Filters {
	/** Free text: ID or keywords. */
	search?: string;
	/** Numeric enums in filters (or 'All' for UI). */
	status?: All<TicketStatus>;
	category?: All<number>;
	priority?: All<Priority>;
	/** Inclusive date range on createdAt (YYYY-MM-DD or full ISO). */
	dateFrom?: string;
	dateTo?: string;

	/** Filter tickets created by a specific user. */
	createdByUserId?: number;

	/** Filter tickets assigned to a specific user; set to null to filter unassigned tickets. */
	assignedToUserId?: number | null;
}

/** Compose the full query for the list endpoint. */
export type TicketQuery = Paging & Sorting<TicketSortableField> & Filters;

/* ===== Query split (paging/sort/filter) - end ========= **/

/* ===== Table config & actions - start ================= **/

export interface TableAction {
	id: string;
	label: string;
	icon?: string; // optional icon identifier (e.g., Bootstrap Icons class)
	requiresSelection?: boolean;
}

export interface TicketsTableConfig {
	role: UserRole;
	columns?: Array<keyof TicketListItem | 'actions'>;
	actions?: TableAction[];
	bulkActions?: TableAction[];
	enableSearch?: boolean;
	enableFilters?: boolean;
	enableExportCsv?: boolean;
}
/* ===== Table config & actions - end ================= **/

export interface CreateTicketResponse {
	ticketId: number;
	status: TicketStatus;
	assignedToUserId?: number;
	createdAt: string;
}

export interface TicketDetail {
	ticketId: number;
	title: string;
	description: string;
	categoryId: number;
	priority: Priority;
	status: TicketStatus;
	createdAt: string;
	updatedAt?: string | null;
	createdById: number;
	assignedToId?: number | null;
	createdByName?: string | null;
	assignedToName?: string | null;
	capabilities?: {
		canEdit?: boolean;
		canEditTitle: boolean;
		canEditDescription: boolean;
		canEditCategory: boolean;
		canEditPriority: boolean;
		canEditStatus: boolean;
		canEditAssignment: boolean;
	};
}

export interface TicketFileDto {
	ticketFileId: number;
	ticketId: number;
	originalName: string;
	contentType: string;
	sizeBytes: number;
	uploaderUserId: number;
	uploaderName?: string; // Added for display in attachments table
	createdAt: string; // ISO
	downloadRoute?: string | null;
	checksumSha256?: string | null;
}

export type TicketHistoryDto = {
	historyId: number;
	ticketId: number;
	changeType: string;
	oldValue: string | null;
	newValue: string | null;
	oldValueDisplay?: string | null;
	newValueDisplay?: string | null;
	changedByName: string;
	changedAt: string;
};

export type TicketCommentDto = {
	commentId: number;
	ticketId: number;
	content: string;
	createdById: number;
	createdByName: string | null;
	createdByRoleId: number;
	createdAt: string;
	isInternal: boolean;
};

export type AddTicketCommentRequest = {
	content: string;
	isInternal?: boolean;
};
