import { describe, it, expect, vi } from 'vitest';

import { createUser, deleteUser, fetchUsers, updateUser } from '$lib/services/Users';
import { UserRole } from '$lib/types/enums';

function makeJsonResponse(body: any, init?: { status?: number; statusText?: string }) {
	const status = init?.status ?? 200;
	const statusText = init?.statusText ?? 'OK';
	return {
		ok: status >= 200 && status < 300,
		status,
		statusText,
		json: async () => body,
		text: async () => JSON.stringify(body)
	} as any;
}

describe('Users service', () => {
	it('fetchUsers builds query and omits role=All', async () => {
		const fetchSpy = vi.fn(async (input: any) => {
			return makeJsonResponse({
				items: [],
				total: 0,
				page: 1,
				size: 10,
				totalPages: 1,
				hasNext: false,
				hasPrevious: false,
				sort: null
			});
		});

		await fetchUsers(
			{
				page: 2,
				pageSize: 25,
				role: 'All' as any,
				search: 'john',
				sortBy: 'email' as any,
				sortDir: 'desc'
			},
			fetchSpy as any
		);

		expect(fetchSpy).toHaveBeenCalledTimes(1);
		const url = String(fetchSpy.mock.calls[0][0]);
		expect(url).toContain('/api/users?');

		const parsed = new URL(url, 'http://localhost');
		expect(parsed.searchParams.get('page')).toBe('2');
		expect(parsed.searchParams.get('pageSize')).toBe('25');
		expect(parsed.searchParams.get('search')).toBe('john');
		expect(parsed.searchParams.get('sortBy')).toBe('email');
		expect(parsed.searchParams.get('sortDir')).toBe('desc');
		expect(parsed.searchParams.get('role')).toBeNull();
	});

	it('createUser throws fieldErrors for ValidationProblemDetails', async () => {
		const fetchSpy = vi.fn(async () => {
			return makeJsonResponse(
				{
					status: 400,
					code: 'VALIDATION_FAILED',
					traceId: 't-1',
					errors: { Email: ['Email is required'] }
				},
				{ status: 400, statusText: 'Bad Request' }
			);
		});

		await expect(
			createUser(
				{ name: '', email: '', password: '', role: UserRole.Employee, categoryId: null },
				fetchSpy as any
			)
		).rejects.toMatchObject({
			code: 'VALIDATION_FAILED',
			traceId: 't-1',
			fieldErrors: { Email: ['Email is required'] }
		});
	});

	it('updateUser throws code from ProblemDetails on non-400 errors', async () => {
		const fetchSpy = vi.fn(async () => {
			return makeJsonResponse(
				{
					status: 409,
					code: 'USER_SELF_DEACTIVATION_NOT_ALLOWED',
					traceId: 't-2',
					detail: 'Cannot deactivate yourself'
				},
				{ status: 409, statusText: 'Conflict' }
			);
		});

		await expect(updateUser(1, { isActive: false }, fetchSpy as any)).rejects.toMatchObject({
			code: 'USER_SELF_DEACTIVATION_NOT_ALLOWED',
			traceId: 't-2'
		});
	});

	it('deleteUser throws code from ProblemDetails on error', async () => {
		const fetchSpy = vi.fn(async () => {
			return makeJsonResponse(
				{
					status: 404,
					code: 'USER_NOT_FOUND',
					traceId: 't-3',
					detail: 'Not found'
				},
				{ status: 404, statusText: 'Not Found' }
			);
		});

		await expect(deleteUser(123, fetchSpy as any)).rejects.toMatchObject({
			code: 'USER_NOT_FOUND',
			traceId: 't-3'
		});
	});
});
