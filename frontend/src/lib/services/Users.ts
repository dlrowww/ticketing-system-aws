import { API_BASE } from '$lib/config';

import type {
	AssignableUserDto,
	CreateUserRequest,
	PagedResult,
	UpdateUserRequest,
	UserDetailsDto,
	UserListItemDto,
	UserQuery
} from '$lib/types/users';

const API = `${API_BASE}/users`;

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

export async function fetchUsers(query: UserQuery, fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<PagedResult<UserListItemDto>> {
	const params = new URLSearchParams();

	if (query.page) params.set('page', String(query.page));
	if (query.pageSize) params.set('pageSize', String(query.pageSize));
	if (query.search) params.set('search', query.search);
	if (query.role && query.role !== 'All') params.set('role', String(query.role));
	if (query.category && query.category !== 'All') params.set('category', String(query.category));
	if (query.isActive && query.isActive !== 'All') params.set('isActive', query.isActive);
	if (query.sortBy) params.set('sortBy', query.sortBy);
	if (query.sortDir) params.set('sortDir', query.sortDir);

	const res = await fetchFn(`${API}?${params.toString()}`, {
		credentials: 'include',
		signal
	});

	if (!res.ok) throw new Error(`Failed to load users: ${res.status} ${res.statusText}`);

	return (await res.json()) as PagedResult<UserListItemDto>;
}

export async function getUserById(id: number, fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<UserDetailsDto> {
	const res = await fetchFn(`${API}/${id}`, { credentials: 'include', signal });
	if (!res.ok) throw new Error(`Failed to load user #${id}: ${res.status} ${res.statusText}`);
	return (await res.json()) as UserDetailsDto;
}

export async function createUser(request: CreateUserRequest, fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<UserDetailsDto> {
	const res = await fetchFn(API, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'include',
		body: JSON.stringify(request),
		signal
	});

	if (res.status === 400) {
		const payload = (await res.json().catch(() => null)) as unknown;

		if (isValidationProblemDetailsPayload(payload) && payload.errors) {
			throw createApiError('Validation failed', {
				code: payload.code,
				traceId: payload.traceId,
				fieldErrors: payload.errors
			});
		}

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
		throw createApiError(`Create user failed: ${res.status} ${res.statusText}`, { code, traceId, detail: text });
	}

	return (await res.json()) as UserDetailsDto;
}

export async function updateUser(id: number, request: UpdateUserRequest, fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<UserDetailsDto> {
	const res = await fetchFn(`${API}/${id}`, {
		method: 'PATCH',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'include',
		body: JSON.stringify(request),
		signal
	});

	if (res.status === 400) {
		const payload = (await res.json().catch(() => null)) as unknown;

		if (isValidationProblemDetailsPayload(payload) && payload.errors) {
			throw createApiError('Validation failed', {
				code: payload.code,
				traceId: payload.traceId,
				fieldErrors: payload.errors
			});
		}

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
		throw createApiError(`Update user failed: ${res.status} ${res.statusText}`, { code, traceId, detail: text });
	}

	return (await res.json()) as UserDetailsDto;
}

export async function deleteUser(id: number, fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<void> {
	const res = await fetchFn(`${API}/${id}`, {
		method: 'DELETE',
		credentials: 'include',
		signal
	});

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Delete user failed: ${res.status} ${res.statusText}`, { code, traceId, detail: text });
	}
}

export async function fetchAssignableUsers(
	ticketId: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<AssignableUserDto[]> {
	const res = await fetchFn(`${API_BASE}/tickets/${ticketId}/assignable-users`, {
		credentials: 'include',
		signal
	});

	if (!res.ok) {
		const text = await res.text().catch(() => '');
		throw createApiError(`Failed to load assignable users: ${res.status} ${res.statusText}`, { detail: text });
	}

	return (await res.json()) as AssignableUserDto[];
}

/**
 * Fetch all users (no pagination) - useful for lookups/mapping
 */
export async function fetchAllUsers(fetchFn: typeof fetch = fetch, signal?: AbortSignal): Promise<UserListItemDto[]> {
	const res = await fetchFn(`${API}?pageSize=1000`, {
		credentials: 'include',
		signal
	});

	if (!res.ok) {
		const text = await res.text().catch(() => '');
		throw createApiError(`Failed to load all users: ${res.status} ${res.statusText}`, { detail: text });
	}

	const paged = (await res.json()) as PagedResult<UserListItemDto>;
	return paged.items;
}
