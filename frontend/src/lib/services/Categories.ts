import { API_BASE } from '$lib/config';

import type {
	CategoryDto,
	CategoryQuery,
	CreateCategoryRequest,
	UpdateCategoryRequest
} from '$lib/types/categories';

const API = `${API_BASE}/categories`;

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
	if (!v || typeof v !== 'object' || Array.isArray(v)) return false;
	// Check if it's a ProblemDetails object (has type, title, status, etc.)
	if ('type' in v || 'title' in v || 'status' in v || 'traceId' in v || 'code' in v) {
		return false;
	}
	// It's a field errors dict if it's an object without ProblemDetails properties
	return true;
}

function createApiError(message: string, extras?: Record<string, unknown>) {
	const err = new Error(message);
	if (extras) Object.assign(err as any, extras);
	return err;
}

export async function fetchCategories(
	query?: CategoryQuery,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<CategoryDto[]> {
	const params = new URLSearchParams();

	if (query?.includeInactive) params.set('includeInactive', 'true');
	if (query?.sortBy) params.set('sortBy', query.sortBy);
	if (query?.sortDir) params.set('sortDir', query.sortDir);

	const url = params.toString() ? `${API}?${params.toString()}` : API;
	const res = await fetchFn(url, {
		credentials: 'include',
		signal
	});

	if (!res.ok)
		throw new Error(`Failed to load categories: ${res.status} ${res.statusText}`);

	return (await res.json()) as CategoryDto[];
}

export async function getCategoryById(
	id: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<CategoryDto> {
	const res = await fetchFn(`${API}/${id}`, { credentials: 'include', signal });
	if (!res.ok)
		throw new Error(`Failed to load category #${id}: ${res.status} ${res.statusText}`);
	return (await res.json()) as CategoryDto;
}

export async function createCategory(
	request: CreateCategoryRequest,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<CategoryDto> {
	const res = await fetchFn(API, {
		method: 'POST',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'include',
		body: JSON.stringify(request),
		signal
	});

	if (res.status === 400) {
		const payload = (await res.json().catch(() => null)) as unknown;
		console.log('[Categories.createCategory] 400 error payload:', payload);

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

		// Handle simple error code from ProblemDetails
		const errorCode = (payload as any)?.code;
		console.log('[Categories.createCategory] Extracted error code:', errorCode);
		throw createApiError('Validation failed', { 
			code: errorCode || 'VALIDATION_FAILED',
			traceId: (payload as any)?.traceId
		});
	}

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Create category failed: ${res.status} ${res.statusText}`, {
			code,
			traceId,
			detail: text
		});
	}

	return (await res.json()) as CategoryDto;
}

export async function updateCategory(
	id: number,
	request: UpdateCategoryRequest,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<CategoryDto> {
	const res = await fetchFn(`${API}/${id}`, {
		method: 'PATCH',
		headers: { 'Content-Type': 'application/json' },
		credentials: 'include',
		body: JSON.stringify(request),
		signal
	});

	// Handle 409 conflict FIRST (category in use)
	if (res.status === 409) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = (payload as any)?.code ?? 'CATEGORY_IN_USE';
		throw createApiError('Category is in use', { code });
	}

	// Handle validation errors (400)
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

		// Handle simple error code from ProblemDetails
		const errorCode = (payload as any)?.code;
		throw createApiError('Validation failed', { 
			code: errorCode || 'VALIDATION_FAILED',
			traceId: (payload as any)?.traceId
		});
	}

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Update category failed: ${res.status} ${res.statusText}`, {
			code,
			traceId,
			detail: text
		});
	}

	return (await res.json()) as CategoryDto;
}

export async function deleteCategory(
	id: number,
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<void> {
	const res = await fetchFn(`${API}/${id}`, {
		method: 'DELETE',
		credentials: 'include',
		signal
	});

	if (res.status === 409) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code ?? 'CATEGORY_IN_USE';
		throw createApiError('Category is in use and cannot be deleted', { code });
	}

	if (!res.ok) {
		const payload = (await res.json().catch(() => null)) as any;
		const code = payload?.code;
		const traceId = payload?.traceId;
		const text = payload?.detail ?? (await res.text().catch(() => ''));
		throw createApiError(`Delete category failed: ${res.status} ${res.statusText}`, {
			code,
			traceId,
			detail: text
		});
	}
}
