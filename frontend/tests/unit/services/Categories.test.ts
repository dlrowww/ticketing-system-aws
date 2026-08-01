import { describe, it, expect, vi, beforeEach } from 'vitest';
import {
	fetchCategories,
	getCategoryById,
	createCategory,
	updateCategory,
	deleteCategory,
	type FieldErrors
} from '$lib/services/Categories';
import type { CategoryDto, CreateCategoryRequest, UpdateCategoryRequest } from '$lib/types/categories';

describe('Categories Service', () => {
	let mockFetch: ReturnType<typeof vi.fn>;

	beforeEach(() => {
		mockFetch = vi.fn();
	});

	describe('fetchCategories', () => {
		it('calls correct endpoint with default params', async () => {
			const mockCategories: CategoryDto[] = [
				{ categoryId: 1, namePl: 'IT', nameEn: 'IT', isActive: true, createdAt: new Date().toISOString(), updatedAt: null },
				{ categoryId: 2, namePl: 'Logistyka', nameEn: 'Logistics', isActive: true, createdAt: new Date().toISOString(), updatedAt: null }
			];

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockCategories
			});

			const result = await fetchCategories(undefined, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith('/api/categories', expect.objectContaining({
				credentials: 'include'
			}));
			expect(result).toEqual(mockCategories);
		});

		it('includes includeInactive query param when set to true', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => []
			});

			await fetchCategories({ includeInactive: true }, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith(
				'/api/categories?includeInactive=true',
				expect.any(Object)
			);
		});

		it('includes sortBy and sortDir query params when provided', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => []
			});

			await fetchCategories({ sortBy: 'namePl', sortDir: 'desc' }, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith(
				'/api/categories?sortBy=namePl&sortDir=desc',
				expect.any(Object)
			);
		});

		it('throws error when response is not ok', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 500,
				statusText: 'Internal Server Error'
			});

			await expect(fetchCategories(undefined, mockFetch)).rejects.toThrow(
				'Failed to load categories: 500 Internal Server Error'
			);
		});
	});

	describe('getCategoryById', () => {
		it('calls correct endpoint with category ID', async () => {
			const mockCategory: CategoryDto = {
				categoryId: 1,
				namePl: 'IT',
				nameEn: 'IT',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				json: async () => mockCategory
			});

			const result = await getCategoryById(1, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith('/api/categories/1', expect.objectContaining({
				credentials: 'include'
			}));
			expect(result).toEqual(mockCategory);
		});

		it('throws error when category not found (404)', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 404,
				statusText: 'Not Found'
			});

			await expect(getCategoryById(999, mockFetch)).rejects.toThrow(
				'Failed to load category #999: 404 Not Found'
			);
		});
	});

	describe('createCategory', () => {
		it('sends correct POST payload to endpoint', async () => {
			const request: CreateCategoryRequest = {
				namePl: 'Finanse',
				nameEn: 'Finance'
			};

			const mockCreated: CategoryDto = {
				categoryId: 4,
				namePl: 'Finanse',
				nameEn: 'Finance',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				status: 201,
				json: async () => mockCreated
			});

			const result = await createCategory(request, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith('/api/categories', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify(request),
				signal: undefined
			});
			expect(result).toEqual(mockCreated);
		});

		it('parses field errors from 400 validation response', async () => {
			const validationPayload = {
				type: 'https://httpstatuses.com/400',
				title: 'Validation failed',
				status: 400,
				code: 'VALIDATION_FAILED',
				traceId: '12345',
				errors: {
					namePl: ['Polish name is required'],
					nameEn: ['English name is too short']
				}
			};

			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 400,
				json: async () => validationPayload
			});

			try {
				await createCategory({ namePl: '', nameEn: 'A' }, mockFetch);
				expect.fail('Should have thrown error');
			} catch (e: any) {
				expect(e.message).toBe('Validation failed');
				expect(e.fieldErrors).toEqual(validationPayload.errors);
				expect(e.code).toBe('VALIDATION_FAILED');
				expect(e.traceId).toBe('12345');
			}
		});

		it('handles 409 conflict error with code', async () => {
			const conflictPayload = {
				type: 'https://httpstatuses.com/409',
				title: 'Conflict',
				status: 409,
				code: 'CATEGORY_NAME_ALREADY_EXISTS',
				detail: 'Polish name already exists',
				traceId: '67890'
			};

			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 409,
				json: async () => conflictPayload,
				text: async () => JSON.stringify(conflictPayload)
			});

			try {
				await createCategory({ namePl: 'IT', nameEn: 'IT' }, mockFetch);
				expect.fail('Should have thrown error');
			} catch (e: any) {
				expect(e.message).toContain('Create category failed');
				expect(e.code).toBe('CATEGORY_NAME_ALREADY_EXISTS');
			}
		});
	});

	describe('updateCategory', () => {
		it('sends correct PATCH payload to endpoint', async () => {
			const request: UpdateCategoryRequest = {
				namePl: 'IT Updated',
				nameEn: 'IT Updated',
				isActive: false
			};

			const mockUpdated: CategoryDto = {
				categoryId: 1,
				namePl: 'IT Updated',
				nameEn: 'IT Updated',
				isActive: false,
				createdAt: new Date().toISOString(),
				updatedAt: new Date().toISOString()
			};

			mockFetch.mockResolvedValueOnce({
				ok: true,
				status: 200,
				json: async () => mockUpdated
			});

			const result = await updateCategory(1, request, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith('/api/categories/1', {
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				credentials: 'include',
				body: JSON.stringify(request),
				signal: undefined
			});
			expect(result).toEqual(mockUpdated);
		});

		it('handles 409 conflict when category is in use', async () => {
			const conflictPayload = {
				code: 'CATEGORY_IN_USE',
				detail: 'Cannot update category that is in use'
			};

			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 409,
				json: async () => conflictPayload
			});

			try {
				await updateCategory(1, { isActive: false }, mockFetch);
				expect.fail('Should have thrown error');
			} catch (e: any) {
				expect(e.message).toBe('Category is in use');
				expect(e.code).toBe('CATEGORY_IN_USE');
			}
		});
	});

	describe('deleteCategory', () => {
		it('calls correct DELETE endpoint', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: true,
				status: 204
			});

			await deleteCategory(5, mockFetch);

			expect(mockFetch).toHaveBeenCalledWith('/api/categories/5', {
				method: 'DELETE',
				credentials: 'include',
				signal: undefined
			});
		});

		it('handles 409 conflict when category is in use', async () => {
			const conflictPayload = {
				code: 'CATEGORY_IN_USE',
				detail: 'Cannot delete category that is in use by tickets or users'
			};

			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 409,
				json: async () => conflictPayload
			});

			try {
				await deleteCategory(1, mockFetch);
				expect.fail('Should have thrown error');
			} catch (e: any) {
				expect(e.message).toBe('Category is in use and cannot be deleted');
				expect(e.code).toBe('CATEGORY_IN_USE');
			}
		});

		it('handles 404 not found error', async () => {
			mockFetch.mockResolvedValueOnce({
				ok: false,
				status: 404,
				json: async () => ({ code: 'CATEGORY_NOT_FOUND' }),
				text: async () => ''
			});

			try {
				await deleteCategory(999, mockFetch);
				expect.fail('Should have thrown error');
			} catch (e: any) {
				expect(e.message).toContain('Delete category failed');
			}
		});
	});
});
