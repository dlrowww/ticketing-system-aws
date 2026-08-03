import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { render, screen, fireEvent, waitFor } from '@testing-library/svelte';
import { toastStore } from '$lib/stores/toast';

// Mock i18n to return the key as-is for predictable assertions
vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

// Mock the Category service
const getCategoryByIdMock = vi.fn();
const createCategoryMock = vi.fn();
const updateCategoryMock = vi.fn();
const deleteCategoryMock = vi.fn();

vi.mock('$lib/services/Categories', () => ({
	getCategoryById: (...args: any[]) => getCategoryByIdMock(...args),
	createCategory: (...args: any[]) => createCategoryMock(...args),
	updateCategory: (...args: any[]) => updateCategoryMock(...args),
	deleteCategory: (...args: any[]) => deleteCategoryMock(...args)
}));

// Mock modalStore
vi.mock('$lib/stores/modal', () => ({
	modalStore: {
		confirm: vi.fn()
	}
}));

import CategoryFormModal from '$lib/components/modals/CategoryFormModal.svelte';

describe('CategoryFormModal', () => {
	beforeEach(() => {
		getCategoryByIdMock.mockReset();
		createCategoryMock.mockReset();
		updateCategoryMock.mockReset();
		deleteCategoryMock.mockReset();
		vi.spyOn(toastStore, 'success').mockImplementation(() => 'test-toast');
		vi.spyOn(toastStore, 'error').mockImplementation(() => 'test-toast');
	});

	afterEach(() => {
		vi.restoreAllMocks();
	});

	describe('Create Mode', () => {
		it('renders create form with empty fields', async () => {
			render(CategoryFormModal as any, { props: {} });

				expect(screen.getByLabelText(/category_name_pl/)).toBeTruthy();
				expect(screen.getByLabelText(/category_name_en/)).toBeTruthy();
				expect(screen.getByText('create')).toBeTruthy();
		});

		it('submits create request with valid data and shows success toast', async () => {
			const createdCategory = {
				categoryId: 4,
				namePl: 'Finanse',
				nameEn: 'Finance',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};
			createCategoryMock.mockResolvedValue(createdCategory);

			const onClose = vi.fn();
			const dispatchSpy = vi.spyOn(window, 'dispatchEvent');

			render(CategoryFormModal as any, { props: { onClose } });

				await fireEvent.input(screen.getByLabelText(/category_name_pl/), {
				target: { value: 'Finanse' }
			});
				await fireEvent.input(screen.getByLabelText(/category_name_en/), {
				target: { value: 'Finance' }
			});

			await fireEvent.submit(screen.getByText('create').closest('form')!);

			expect(createCategoryMock).toHaveBeenCalledWith(
				{ namePl: 'Finanse', nameEn: 'Finance' },
				fetch,
				expect.any(AbortSignal)
			);

			await waitFor(() => {
				expect(toastStore.success).toHaveBeenCalledWith('category_create_success');
			});

				await waitFor(() => {
					const refreshCall = dispatchSpy.mock.calls.find(
						(c) => (c[0] as Event).type === 'categories:refresh'
					);
					expect(refreshCall).toBeTruthy();
				});
		});

		it('displays field errors from backend validation failure', async () => {
			createCategoryMock.mockRejectedValue({
				message: 'Validation failed',
				fieldErrors: {
						NamePl: ['CATEGORY_NAME_TOO_SHORT'],
						NameEn: ['CATEGORY_NAME_REQUIRED']
				}
			});

			render(CategoryFormModal as any);

			await fireEvent.input(screen.getByLabelText(/category_name_pl/), { target: { value: 'A' } });
			await fireEvent.input(screen.getByLabelText(/category_name_en/), { target: { value: '' } });
			await fireEvent.submit(screen.getByText('create').closest('form')!);

			await waitFor(() => {
				expect(screen.getByText('error_code_CATEGORY_NAME_TOO_SHORT')).toBeTruthy();
				expect(screen.getByText('error_code_CATEGORY_NAME_REQUIRED')).toBeTruthy();
			});
		});
	});

	describe('Edit Mode', () => {
		it('loads category details when categoryId is provided', async () => {
			const existingCategory = {
				categoryId: 2,
				namePl: 'Logistyka',
				nameEn: 'Logistics',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};
			getCategoryByIdMock.mockResolvedValue(existingCategory);

			render(CategoryFormModal as any, { props: { categoryId: 2 } });

			expect(getCategoryByIdMock).toHaveBeenCalledWith(2, fetch, expect.any(AbortSignal));

			await waitFor(() => {
				const namePlInput = screen.getByLabelText(/category_name_pl/) as HTMLInputElement;
				const nameEnInput = screen.getByLabelText(/category_name_en/) as HTMLInputElement;
				expect(namePlInput.value).toBe('Logistyka');
				expect(nameEnInput.value).toBe('Logistics');
			});
		});

		it('submits update request with changed data', async () => {
			const existingCategory = {
				categoryId: 2,
				namePl: 'Logistyka',
				nameEn: 'Logistics',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};
			getCategoryByIdMock.mockResolvedValue(existingCategory);

			const updatedCategory = {
				...existingCategory,
				namePl: 'Logistyka Updated',
				nameEn: 'Logistics Updated',
				updatedAt: new Date().toISOString()
			};
			updateCategoryMock.mockResolvedValue(updatedCategory);

			const onClose = vi.fn();
			render(CategoryFormModal as any, { props: { categoryId: 2, onClose } });

			await waitFor(() => {
				const namePlInput = screen.getByLabelText(/category_name_pl/) as HTMLInputElement;
				expect(namePlInput.value).toBe('Logistyka');
			});

			await fireEvent.input(screen.getByLabelText(/category_name_pl/), {
				target: { value: 'Logistyka Updated' }
			});
			await fireEvent.input(screen.getByLabelText(/category_name_en/), {
				target: { value: 'Logistics Updated' }
			});

			await fireEvent.submit(screen.getByText('save').closest('form')!);

			expect(updateCategoryMock).toHaveBeenCalledWith(
				2,
				{ namePl: 'Logistyka Updated', nameEn: 'Logistics Updated', isActive: true },
				fetch,
				expect.any(AbortSignal)
			);

			await waitFor(() => {
				expect(toastStore.success).toHaveBeenCalledWith('category_update_success');
			});
		});

		it('handles update conflict error (409)', async () => {
			const existingCategory = {
				categoryId: 2,
				namePl: 'Logistyka',
				nameEn: 'Logistics',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};
			getCategoryByIdMock.mockResolvedValue(existingCategory);

			updateCategoryMock.mockRejectedValue({
				message: 'Category is in use',
				code: 'CATEGORY_IN_USE'
			});

			render(CategoryFormModal as any, { props: { categoryId: 2 } });

			await waitFor(() => {
				const namePlInput = screen.getByLabelText(/category_name_pl/) as HTMLInputElement;
				expect(namePlInput.value).toBe('Logistyka');
			});

			await fireEvent.input(screen.getByLabelText(/category_name_pl/), {
				target: { value: 'Logistyka Updated' }
			});
			await fireEvent.submit(screen.getByText('save').closest('form')!);

			await waitFor(() => {
				expect(screen.getByText('error_code_CATEGORY_IN_USE')).toBeTruthy();
			});
		});
	});

	describe('Delete Functionality', () => {
		it('shows delete button only in edit mode', async () => {
			const existingCategory = {
				categoryId: 2,
				namePl: 'Logistyka',
				nameEn: 'Logistics',
				isActive: true,
				createdAt: new Date().toISOString(),
				updatedAt: null
			};
			getCategoryByIdMock.mockResolvedValue(existingCategory);

			// Edit mode
			const { unmount } = render(CategoryFormModal as any, { props: { categoryId: 2 } });
			await waitFor(() => {
				expect(screen.getByText('delete')).toBeTruthy();
			});
			unmount();

			// Create mode - delete button should not exist
			render(CategoryFormModal as any, { props: {} });
			expect(() => screen.getByText('delete')).toThrow();
		});
	});
});
