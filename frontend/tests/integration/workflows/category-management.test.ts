// @vitest-environment node
import { describe, it, expect, vi, beforeEach } from 'vitest';
import { UserRole } from '$lib/types/enums';
import { load as loadCategories } from '../../../src/routes/app/admin/categories/+page.server';
import type { CategoryDto } from '$lib/types/categories';

type ParentData = {
	user: {
		id: string;
		roleId: string;
		categoryId?: string;
	};
};

function makeUrl(path: string, qs: string = ''): URL {
	return new URL(`http://localhost${path}${qs ? `?${qs}` : ''}`);
}

function makeParent(user: ParentData['user']) {
	return async () => ({ user });
}

function makeLocals(user: ParentData['user']) {
	return { user };
}

function createFetchSpy(mockResponse?: any) {
	return vi.fn(async (input: RequestInfo | URL) => {
		return {
			ok: true,
			status: 200,
			statusText: 'OK',
			json: async () => mockResponse ?? []
		} as any;
	});
}

function fetchUrls(fetchSpy: ReturnType<typeof createFetchSpy>): string[] {
	return (fetchSpy as any).mock.calls.map((c: any[]) => String(c[0]));
}

function expectQueryParam(url: string, key: string, value: string) {
	const parsed = new URL(url, 'http://localhost');
	expect(parsed.searchParams.get(key)).toBe(value);
}

describe('Phase T.4 - Category Management Integration Tests', () => {
	const mockCategories: CategoryDto[] = [
		{
			categoryId: 1,
			namePl: 'IT',
			nameEn: 'IT',
			isActive: true,
			createdAt: '2025-01-01T00:00:00Z',
			updatedAt: '2025-01-01T00:00:00Z',
			ticketCount: 10
		},
		{
			categoryId: 2,
			namePl: 'Logistyka',
			nameEn: 'Logistics',
			isActive: true,
			createdAt: '2025-01-02T00:00:00Z',
			updatedAt: '2025-01-02T00:00:00Z',
			ticketCount: 5
		},
		{
			categoryId: 3,
			namePl: 'Administracja',
			nameEn: 'Administrative',
			isActive: false,
			createdAt: '2025-01-03T00:00:00Z',
			updatedAt: '2025-01-03T00:00:00Z',
			ticketCount: 0
		}
	];

	describe('Access Control & Loading', () => {
		it('redirects non-authenticated users to login', async () => {
			const fetchSpy = createFetchSpy();
			await expect(
				loadCategories({
					fetch: fetchSpy as any,
					url: makeUrl('/app/admin/categories'),
					locals: { user: null } as any,
					parent: makeParent({ id: '', roleId: '' })
				} as any)
			).rejects.toMatchObject({ status: 302, location: '/login' });
		});

		it('redirects non-Admin users to dashboard', async () => {
			const fetchSpy = createFetchSpy();
			await expect(
				loadCategories({
					fetch: fetchSpy as any,
					url: makeUrl('/app/admin/categories'),
					locals: makeLocals({ id: '10', roleId: String(UserRole.Employee) }),
					parent: makeParent({ id: '10', roleId: String(UserRole.Employee) })
				} as any)
			).rejects.toMatchObject({ status: 302, location: '/app/dashboard' });
		});

		it('redirects TeamLeader users to dashboard', async () => {
			const fetchSpy = createFetchSpy();
			await expect(
				loadCategories({
					fetch: fetchSpy as any,
					url: makeUrl('/app/admin/categories'),
					locals: makeLocals({ id: '5', roleId: String(UserRole.TeamLeader), categoryId: '1' }),
					parent: makeParent({ id: '5', roleId: String(UserRole.TeamLeader), categoryId: '1' })
				} as any)
			).rejects.toMatchObject({ status: 302, location: '/app/dashboard' });
		});

		it('loads and displays categories for Admin users', async () => {
			const fetchSpy = createFetchSpy(mockCategories);
			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial).toHaveLength(3);
			expect(result.initial).toEqual(mockCategories);
			expect(result.query).toEqual({
				includeInactive: false,
				sortBy: 'categoryId',
				sortDir: 'asc'
			});

			const urls = fetchUrls(fetchSpy);
			expect(urls).toHaveLength(1);
			expect(urls[0]).toContain('/api/categories');
		});
	});

	describe('Query Parameter Handling', () => {
		it('applies includeInactive filter from query string', async () => {
			const fetchSpy = createFetchSpy(mockCategories);
			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories', 'includeInactive=true'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.query?.includeInactive).toBe(true);

			const urls = fetchUrls(fetchSpy);
			expect(urls[0]).toContain('includeInactive=true');
		});

		it('applies sortBy and sortDir from query string', async () => {
			const fetchSpy = createFetchSpy(mockCategories);
			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories', 'sortBy=namePl&sortDir=desc'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.query?.sortBy).toBe('namePl');
			expect(result.query?.sortDir).toBe('desc');

			const urls = fetchUrls(fetchSpy);
			expectQueryParam(urls[0], 'sortBy', 'namePl');
			expectQueryParam(urls[0], 'sortDir', 'desc');
		});

		it('uses default query params when none provided', async () => {
			const fetchSpy = createFetchSpy(mockCategories);
			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.query).toEqual({
				includeInactive: false,
				sortBy: 'categoryId',
				sortDir: 'asc'
			});
		});

		it('combines multiple query parameters correctly', async () => {
			const fetchSpy = createFetchSpy(mockCategories);
			await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories', 'includeInactive=true&sortBy=nameEn&sortDir=asc'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			const urls = fetchUrls(fetchSpy);
			const apiUrl = urls[0];
			expectQueryParam(apiUrl, 'includeInactive', 'true');
			expectQueryParam(apiUrl, 'sortBy', 'nameEn');
			expectQueryParam(apiUrl, 'sortDir', 'asc');
		});
	});

	describe('Error Handling', () => {
		it('handles fetch errors gracefully', async () => {
			const fetchSpy = vi.fn(async () => {
				throw new Error('Network error');
			});

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial).toEqual([]);
			expect(result.error).toBe('Failed to load categories. Please try again later.');
			expect(result.query).toEqual({
				includeInactive: false,
				sortBy: 'categoryId',
				sortDir: 'asc'
			});
		});

		it('handles API error responses', async () => {
			const fetchSpy = vi.fn(async () => ({
				ok: false,
				status: 500,
				statusText: 'Internal Server Error',
				json: async () => ({ error: 'Database error' })
			}));

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial).toEqual([]);
			expect(result.error).toBe('Failed to load categories. Please try again later.');
		});
	});

	describe('Data Flow & Integration', () => {
		it('returns active categories only by default', async () => {
			const activeOnly = mockCategories.filter((c) => c.isActive);
			const fetchSpy = createFetchSpy(activeOnly);

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial).toHaveLength(2);
			expect(result.initial.every((c) => c.isActive)).toBe(true);
		});

		it('returns all categories when includeInactive is true', async () => {
			const fetchSpy = createFetchSpy(mockCategories);

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories', 'includeInactive=true'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial).toHaveLength(3);
			expect(result.initial.some((c) => !c.isActive)).toBe(true);
		});

		it('preserves category metadata (ticketCount, timestamps)', async () => {
			const fetchSpy = createFetchSpy(mockCategories);

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			const firstCategory = result.initial[0];
			expect(firstCategory).toHaveProperty('ticketCount');
			expect(firstCategory).toHaveProperty('createdAt');
			expect(firstCategory).toHaveProperty('updatedAt');
			expect(firstCategory.ticketCount).toBe(10);
		});

		it('correctly formats category data for frontend consumption', async () => {
			const fetchSpy = createFetchSpy(mockCategories);

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			result.initial.forEach((category) => {
				expect(category).toHaveProperty('categoryId');
				expect(category).toHaveProperty('namePl');
				expect(category).toHaveProperty('nameEn');
				expect(category).toHaveProperty('isActive');
				expect(typeof category.categoryId).toBe('number');
				expect(typeof category.namePl).toBe('string');
				expect(typeof category.nameEn).toBe('string');
				expect(typeof category.isActive).toBe('boolean');
			});
		});
	});

	describe('Sorting Behavior', () => {
		it('sorts by categoryId ascending by default', async () => {
			const sorted = [...mockCategories].sort((a, b) => a.categoryId - b.categoryId);
			const fetchSpy = createFetchSpy(sorted);

			const result = await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			expect(result.initial[0].categoryId).toBe(1);
			expect(result.initial[1].categoryId).toBe(2);
			expect(result.initial[2].categoryId).toBe(3);
		});

		it('applies custom sort parameters to API request', async () => {
			const fetchSpy = createFetchSpy(mockCategories);

			await loadCategories({
				fetch: fetchSpy as any,
				url: makeUrl('/app/admin/categories', 'sortBy=namePl&sortDir=desc'),
				locals: makeLocals({ id: '1', roleId: String(UserRole.Admin) }),
				parent: makeParent({ id: '1', roleId: String(UserRole.Admin) })
			} as any);

			const urls = fetchUrls(fetchSpy);
			const apiUrl = new URL(urls[0], 'http://localhost');

			expect(apiUrl.searchParams.get('sortBy')).toBe('namePl');
			expect(apiUrl.searchParams.get('sortDir')).toBe('desc');
		});
	});
});
