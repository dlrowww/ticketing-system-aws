import type {
	AddTicketCommentRequest,
	CreateTicketResponse,
	PagedResult,
	TicketCommentDto,
	TicketDetail,
	TicketHistoryDto,
	TicketListItem,
	TicketQuery
} from '$lib/types/tickets';

import { API_BASE } from '$lib/config';
import { TicketStatus, Priority } from '$lib/types/tickets';
import type { TicketFileDto } from '$lib/types/tickets';

const API = `${API_BASE}/tickets`;

/** Fetch a paged list of tickets from backend. */
export async function fetchTickets(
	query: TicketQuery,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<PagedResult<TicketListItem>> {
	const params = new URLSearchParams();

	// Paging
	if (query.page) params.set('page', String(query.page));
	if (query.pageSize) params.set('pageSize', String(query.pageSize));

	// Search
	if (query.search) params.set('search', query.search);

	// Filters: only set if not 'All'
	if (query.status && query.status !== 'All') params.set('status', String(query.status));
	if (query.category && query.category !== 'All') params.set('category', String(query.category));
	if (query.priority && query.priority !== 'All') params.set('priority', String(query.priority));
	if (query.dateFrom) params.set('dateFrom', query.dateFrom);
	if (query.dateTo) params.set('dateTo', query.dateTo);

	// Role-based filters
	if (typeof query.createdByUserId === 'number') {
		params.set('createdByUserId', String(query.createdByUserId));
	}
	if ('assignedToUserId' in query) {
		if (query.assignedToUserId === null) {
			// Explicitly request unassigned tickets (API interprets empty value as "assignedTo IS NULL")
			params.set('assignedToUserId', '');
		} else if (typeof query.assignedToUserId === 'number') {
			params.set('assignedToUserId', String(query.assignedToUserId));
		}
	}

	// Sorting
	if (query.sortBy) params.set('sortBy', query.sortBy);
	if (query.sortDir) params.set('sortDir', query.sortDir);

	const res = await fetchFn(`${API}?${params.toString()}`, {
		credentials: 'include',
		signal
	});

	if (!res.ok) throw new Error(`Failed to load tickets: ${res.statusText}`);

	return (await res.json()) as PagedResult<TicketListItem>;
}

/** Request CSV export for the given query. */
export async function exportTicketsCsv(
	query: TicketQuery,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<Blob> {
	const params = new URLSearchParams();

	if (query.search) params.set('search', query.search);
	if (query.status && query.status !== 'All') params.set('status', String(query.status));
	if (query.category && query.category !== 'All') params.set('category', String(query.category));
	if (query.priority && query.priority !== 'All') params.set('priority', String(query.priority));
	if (query.dateFrom) params.set('dateFrom', query.dateFrom);
	if (query.dateTo) params.set('dateTo', query.dateTo);

	if (typeof query.createdByUserId === 'number') {
		params.set('createdByUserId', String(query.createdByUserId));
	}
	if ('assignedToUserId' in query) {
		if (query.assignedToUserId === null) {
			params.set('assignedToUserId', '');
		} else if (typeof query.assignedToUserId === 'number') {
			params.set('assignedToUserId', String(query.assignedToUserId));
		}
	}
	if (query.sortBy) params.set('sortBy', query.sortBy);
	if (query.sortDir) params.set('sortDir', query.sortDir);

	const res = await fetchFn(`${API}/export?${params.toString()}`, {
		credentials: 'include',
		signal
	});

	if (!res.ok) throw new Error(`Failed to export CSV: ${res.status} ${res.statusText}`);

	return await res.blob();
}

export type FieldErrors = Record<string, string[]>;

type ValidationProblemDetailsPayload = {
	status?: number;
	title?: string;
	detail?: string;
	instance?: string;
	type?: string;
	traceId?: string;
	code?: string;
	errors?: Record<string, string[]>;
};

function isValidationProblemDetailsPayload(v: unknown): v is ValidationProblemDetailsPayload {
	return !!v && typeof v === 'object' && 'errors' in (v as any);
}

function isFieldErrorsDict(v: unknown): v is FieldErrors {
	return !!v && typeof v === 'object' && !Array.isArray(v);
}

function createApiError(message: string, extras?: Record<string, unknown>) {
	const err = new Error(message);
	if (extras) Object.assign(err as any, extras);
	return err;
}

export async function createTicket(
	form: FormData,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<CreateTicketResponse> {
	const res = await fetchFn(API, {
		method: 'POST',
		body: form,
		signal,
		credentials: 'include' // keep cookies/session
		// DO NOT set Content-Type; the browser will set proper multipart boundary
	});

	if (res.status === 400) {
		const payload = (await res.json().catch(() => null)) as unknown;

		// Preferred shape: RFC7807 ValidationProblemDetails with per-field error codes
		if (isValidationProblemDetailsPayload(payload) && payload.errors) {
			throw createApiError('Validation failed', {
				code: payload.code,
				traceId: payload.traceId,
				fieldErrors: payload.errors
			});
		}

		// Back-compat: direct dictionary { field: [codes...] }
		if (isFieldErrorsDict(payload)) {
			throw createApiError('Validation failed', { fieldErrors: payload });
		}

		throw createApiError('Validation failed', { code: 'VALIDATION_FAILED' });
	}

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Create ticket failed: ${res.status} ${res.statusText}`, {
			code,
			traceId,
			detail: text
		});
	}

	return (await res.json()) as CreateTicketResponse;
}

export async function getTicketById(
	id: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
) {
	const res = await fetchFn(`${API}/${id}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok) {
		throw new Error(`Failed to load ticket #${id}: ${res.status} ${res.statusText}`);
	}
	return (await res.json()) as TicketDetail;
}

export interface UpdateTicketRequest {
	title?: string;
	description?: string;
	category?: number;
	priority?: number;
	status?: number;
	assignedToUserId?: number | null;
	clearAssignment?: boolean;
}

export async function updateTicket(
	id: number,
	request: UpdateTicketRequest,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketDetail> {
	const res = await fetchFn(`${API}/${id}`, {
		method: 'PATCH',
		headers: {
			'Content-Type': 'application/json'
		},
		credentials: 'include',
		signal,
		body: JSON.stringify(request)
	});

	if (res.status === 400) {
		const payload = (await res.json().catch(() => null)) as unknown;

		// Check for field-level validation errors
		if (
			isValidationProblemDetailsPayload(payload) &&
			payload.errors &&
			Object.keys(payload.errors).length > 0
		) {
			throw createApiError(payload.detail ?? 'Validation failed', {
				code: payload.code,
				traceId: payload.traceId,
				fieldErrors: payload.errors
			});
		}

		// Single validation error (resource-level) - extract code and detail
		const detail = (payload as any)?.detail ?? 'Validation failed';
		const code = (payload as any)?.code ?? 'VALIDATION_FAILED';
		const traceId = (payload as any)?.traceId;
		throw createApiError(detail, { code, traceId });
	}

	if (res.status === 403) {
		const payload = (await res.json().catch(() => null)) as any;
		const detail = payload?.detail ?? 'You do not have permission to edit this ticket';
		throw createApiError(detail, {
			code: payload?.code ?? 'FORBIDDEN_OPERATION',
			traceId: payload?.traceId
		});
	}

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Update ticket failed: ${res.status} ${res.statusText}`, {
			code,
			traceId,
			detail: text
		});
	}

	return (await res.json()) as TicketDetail;
}

// List attachments for a ticket
export async function listTicketFiles(
	ticketId: number,
	fetchFn: typeof fetch = fetch
): Promise<TicketFileDto[]> {
	const res = await fetchFn(`${API}/${ticketId}/files`, { credentials: 'include' });
	if (!res.ok) throw new Error(`Failed to list files: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketFileDto[];
}

export async function listTicketHistory(
	ticketId: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketHistoryDto[]> {
	const res = await fetchFn(`${API}/${ticketId}/history`, { credentials: 'include', signal });
	if (!res.ok) throw new Error(`Failed to load history: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketHistoryDto[];
}

export async function listTicketComments(
	ticketId: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketCommentDto[]> {
	const res = await fetchFn(`${API}/${ticketId}/comments`, { credentials: 'include', signal });
	if (!res.ok) throw new Error(`Failed to load comments: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketCommentDto[];
}

export async function addTicketComment(
	ticketId: number,
	request: AddTicketCommentRequest,
	fetchFn: typeof fetch = fetch
): Promise<TicketCommentDto> {
	const res = await fetchFn(`${API}/${ticketId}/comments`, {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		credentials: 'include',
		body: JSON.stringify({
			content: request.content,
			isInternal: request.isInternal ?? false
		})
	});

	if (!res.ok) throw new Error(`Failed to add comment: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketCommentDto;
}

export async function addTicketAttachments(
	ticketId: number,
	files: File[],
	fetchFn: typeof fetch = fetch
): Promise<TicketFileDto[]> {
	const formData = new FormData();
	files.forEach((file) => {
		formData.append('files', file);
	});

	const res = await fetchFn(`${API}/${ticketId}/files`, {
		method: 'POST',
		credentials: 'include',
		body: formData
	});

	if (!res.ok) {
		// Provide user-friendly error messages
		if (res.status === 401) {
			throw new Error('You must be logged in to upload files');
		}
		if (res.status === 403) {
			throw new Error('You do not have permission to upload files to this ticket');
		}
		if (res.status === 404) {
			throw new Error('Ticket not found');
		}
		if (res.status === 413) {
			throw new Error('Files are too large. Maximum total size is 50 MB');
		}

		// Try to get error message from backend
		try {
			const errorData = await res.json();
			if (errorData?.detail) {
				throw new Error(errorData.detail);
			}
			if (errorData?.title) {
				throw new Error(errorData.title);
			}
		} catch {
			// If JSON parsing fails, use generic message
		}

		// Generic error message for other cases
		throw new Error('Failed to upload files. Please try again or contact support.');
	}
	return (await res.json()) as TicketFileDto[];
}

// Build a download URL (use the API-provided route if present)
export function ticketFileDownloadUrl(
	ticketId: number,
	fileId: number,
	downloadRoute?: string | null,
	inline = false
) {
	// Normalize base (e.g., http://localhost:5192/api) > no trailing slash
	const base = API_BASE.replace(/\/+$/, '');

	let url: string;
	// If BE gave us a route (e.g., "/api/tickets/10/files/3"), use it
	if (downloadRoute) {
		// Avoid double "/api/api"
		url = downloadRoute.startsWith('http')
			? downloadRoute
			: base.endsWith('/api') && downloadRoute.startsWith('/api')
				? `${base.replace(/\/api$/, '')}${downloadRoute}`
				: `${base}${downloadRoute}`;
	} else {
		url = base.endsWith('/api')
			? `${base}/tickets/${ticketId}/files/${fileId}`
			: `${base}/api/tickets/${ticketId}/files/${fileId}`;
	}

	// Add inline query parameter for preview (prevents download)
	if (inline) {
		const separator = url.includes('?') ? '&' : '?';
		url = `${url}${separator}inline=true`;
	}

	return url;
}

/**
 * Get list of allowed status transitions for a ticket.
 * Always includes the current status itself.
 */
export async function getAllowedStatuses(
	ticketId: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<number[]> {
	const res = await fetchFn(`${API}/${ticketId}/allowed-statuses`, {
		method: 'GET',
		credentials: 'include',
		signal
	});

	if (!res.ok) {
		if (res.status === 401) {
			throw new Error('Unauthorized: Please log in');
		}
		if (res.status === 404) {
			throw new Error('Ticket not found');
		}
		throw new Error(`Failed to fetch allowed statuses: ${res.statusText}`);
	}

	const data = (await res.json()) as { allowedStatuses: number[] };
	return data.allowedStatuses ?? [];
}
