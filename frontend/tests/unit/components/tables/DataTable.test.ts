import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import DataTable from '$lib/components/tables/base/DataTable.svelte';
import type { DataTableConfig, PaginationInfo, Sorting } from '$lib/types/table';
import type { Component } from 'svelte';

// Mock getMessage
vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

// Sample test data
interface TestItem {
	id: number;
	name: string;
	status: string;
	value: number;
}

const mockData: TestItem[] = [
	{ id: 1, name: 'Item 1', status: 'active', value: 100 },
	{ id: 2, name: 'Item 2', status: 'inactive', value: 200 },
	{ id: 3, name: 'Item 3', status: 'active', value: 300 }
];

const mockConfig: DataTableConfig<TestItem> = {
	keyField: 'id',
	columns: [
		{ key: 'id', label: 'ID', sortable: true },
		{ key: 'name', label: 'Name', sortable: true },
		{ key: 'status', label: 'Status', sortable: false },
		{ key: 'value', label: 'Value', sortable: true, formatter: (val) => `$${val}` }
	],
	actions: [{ id: 'view', label: 'View', icon: 'bi-eye' }],
	enableSelection: true,
	enableSorting: true
};

const mockPagination: PaginationInfo = {
	currentPage: 1,
	totalPages: 3,
	pageSize: 10,
	totalItems: 30,
	hasNext: true,
	hasPrevious: false
};

const mockSorting: Sorting = {
	sortBy: 'id',
	sortDir: 'asc'
};

describe('DataTable', () => {
	// Cast DataTable to work with TestItem type for testing
	const TestDataTable = DataTable as unknown as Component<{
		config: DataTableConfig<TestItem>;
		data?: TestItem[];
		pagination?: PaginationInfo;
		sorting?: Sorting;
		loading?: boolean;
		error?: string | null;
		onRowAction?: (event: any) => void;
		onBulkAction?: (event: any) => void;
		onSortChange?: (sortBy: string, sortDir: 'asc' | 'desc') => void;
		onPageChange?: (page: number) => void;
		onPageSizeChange?: (size: number) => void;
	}>;

	describe('Rendering', () => {
		it('renders table with data', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			// Check headers
			expect(screen.getByText('ID')).toBeTruthy();
			expect(screen.getByText('Name')).toBeTruthy();
			expect(screen.getByText('Status')).toBeTruthy();
			expect(screen.getByText('Value')).toBeTruthy();

			// Check data
			expect(screen.getByText('Item 1')).toBeTruthy();
			expect(screen.getByText('Item 2')).toBeTruthy();
			expect(screen.getByText('Item 3')).toBeTruthy();
		});

		it('applies custom formatter to column', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			// Check formatted values
			expect(screen.getByText('$100')).toBeTruthy();
			expect(screen.getByText('$200')).toBeTruthy();
			expect(screen.getByText('$300')).toBeTruthy();
		});

		it('renders loading state', () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: [],
					loading: true
				}
			});

			// Current loading UX uses skeleton placeholders (no "loading" text)
			const skeletons = container.querySelectorAll('.skeleton[aria-busy="true"]');
			expect(skeletons.length).toBeGreaterThan(0);
		});

		it('renders error state', () => {
			const errorMessage = 'Failed to load data';
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: [],
					error: errorMessage
				}
			});

			expect(screen.getByText(errorMessage)).toBeTruthy();
		});

		it('renders empty state', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: []
				}
			});

			expect(screen.getByText('no_results')).toBeTruthy();
		});

		it('renders action buttons', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			const viewButtons = screen.getAllByRole('button', { name: 'View' });
			expect(viewButtons.length).toBe(3); // One for each row
		});
	});

	describe('Selection', () => {
		it('renders selection checkboxes when enabled', () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			expect(checkboxes.length).toBe(4); // 1 select-all + 3 rows
		});

		it('does not render selection checkboxes when disabled', () => {
			const configNoSelection = { ...mockConfig, enableSelection: false };
			const { container } = render(TestDataTable, {
				props: {
					config: configNoSelection,
					data: mockData
				}
			});

			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			expect(checkboxes.length).toBe(0);
		});

		it('selects individual rows', async () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			const firstRowCheckbox = checkboxes[1] as HTMLInputElement; // Skip select-all

			await fireEvent.click(firstRowCheckbox);
			expect(firstRowCheckbox.checked).toBe(true);
		});

		it('select-all checkbox selects all rows', async () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			const selectAllCheckbox = checkboxes[0] as HTMLInputElement;

			await fireEvent.click(selectAllCheckbox);

			// All row checkboxes should be checked
			for (let i = 1; i < checkboxes.length; i++) {
				expect((checkboxes[i] as HTMLInputElement).checked).toBe(true);
			}
		});
	});

	describe('Sorting', () => {
		it('displays sort indicator for sorted column', () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					sorting: mockSorting
				}
			});

			// Should have chevron icon for sorted column
			const chevronIcons = container.querySelectorAll('.bi-chevron-up');
			expect(chevronIcons.length).toBeGreaterThan(0);
		});

		it('calls onSortChange when sortable column is clicked', async () => {
			const onSortChange = vi.fn();
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					sorting: mockSorting,
					onSortChange
				}
			});

			const nameHeader = screen.getByText('Name');
			await fireEvent.click(nameHeader);

			expect(onSortChange).toHaveBeenCalledWith('name', 'asc');
		});

		it('toggles sort direction on repeated clicks', async () => {
			const onSortChange = vi.fn();
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					sorting: { sortBy: 'name', sortDir: 'asc' },
					onSortChange
				}
			});

			const nameHeader = screen.getByText('Name');
			await fireEvent.click(nameHeader);

			// Should toggle to desc when already sorted asc
			expect(onSortChange).toHaveBeenCalledWith('name', 'desc');
		});

		it('does not call onSortChange for non-sortable columns', async () => {
			const onSortChange = vi.fn();
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					sorting: mockSorting,
					onSortChange
				}
			});

			const statusHeader = screen.getByText('Status');
			await fireEvent.click(statusHeader);

			expect(onSortChange).not.toHaveBeenCalled();
		});
	});

	describe('Actions', () => {
		it('calls onRowAction when action button is clicked', async () => {
			const onRowAction = vi.fn();
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					onRowAction
				}
			});

			const viewButtons = screen.getAllByRole('button', { name: 'View' });
			await fireEvent.click(viewButtons[0]);

			expect(onRowAction).toHaveBeenCalledWith({ action: 'view', id: 1 });
		});

		it('renders bulk actions bar when rows are selected', async () => {
			const configWithBulk = {
				...mockConfig,
				bulkActions: [{ id: 'delete', label: 'Delete', icon: 'bi-trash' }]
			};

			const { container } = render(TestDataTable, {
				props: {
					config: configWithBulk,
					data: mockData
				}
			});

			// Select first row
			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			await fireEvent.click(checkboxes[1]);

			// Bulk actions bar should appear
			expect(screen.getByText('selected')).toBeTruthy();
		});

		it('calls onBulkAction when bulk action button is clicked', async () => {
			const onBulkAction = vi.fn();
			const configWithBulk = {
				...mockConfig,
				bulkActions: [{ id: 'delete', label: 'Delete', icon: 'bi-trash' }]
			};

			const { container } = render(TestDataTable, {
				props: {
					config: configWithBulk,
					data: mockData,
					onBulkAction
				}
			});

			// Select first row
			const checkboxes = container.querySelectorAll('input[type="checkbox"]');
			await fireEvent.click(checkboxes[1]);

			// Click bulk delete button
			const deleteButton = screen.getByText('Delete');
			await fireEvent.click(deleteButton);

			expect(onBulkAction).toHaveBeenCalledWith({ action: 'delete', ids: [1] });
		});
	});

	describe('Pagination', () => {
		it('renders pagination when provided', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					pagination: mockPagination,
					onPageChange: vi.fn(),
					onPageSizeChange: vi.fn()
				}
			});

			expect(screen.getByText(/rows_per_page/)).toBeTruthy();
		});

		it('does not render pagination when not provided', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			expect(screen.queryByText('rows_per_page')).toBeNull();
		});
	});

	describe('Accessibility', () => {
		it('has proper ARIA attributes for sortable columns', () => {
			const { container } = render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData,
					sorting: mockSorting
				}
			});

			const headers = container.querySelectorAll('[role="columnheader"]');
			expect(headers.length).toBeGreaterThan(0);

			// Check that sorted column has aria-sort
			const sortedHeaders = Array.from(headers).filter(
				(h) => h.getAttribute('aria-sort') === 'ascending'
			);
			expect(sortedHeaders.length).toBeGreaterThan(0);
		});

		it('has aria-label on action buttons', () => {
			render(TestDataTable, {
				props: {
					config: mockConfig,
					data: mockData
				}
			});

			const viewButtons = screen.getAllByLabelText('View');
			expect(viewButtons.length).toBe(3);
		});
	});
});
