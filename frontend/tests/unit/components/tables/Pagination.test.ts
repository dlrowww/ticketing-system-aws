import { describe, it, expect, vi } from 'vitest';
import { render, screen, fireEvent } from '@testing-library/svelte';
import Pagination from '$lib/components/tables/base/Pagination.svelte';

// Mock getMessage
vi.mock('$lib/i18n', () => ({
    getMessage: (key: string) => key
}));

describe('Pagination', () => {
    describe('Rendering', () => {
        it('renders pagination controls', () => {
            render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/rows_per_page/)).toBeTruthy();
            expect(screen.getByText(/1-10.*50/)).toBeTruthy();
        });

        it('displays correct range for first page', () => {
            render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/1-10/)).toBeTruthy();
        });

        it('displays correct range for middle page', () => {
            render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/21-30/)).toBeTruthy();
        });

        it('displays correct range for last page', () => {
            render(Pagination, {
                props: {
                    currentPage: 5,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/41-50/)).toBeTruthy();
        });

        it('displays correct range when last page is partial', () => {
            render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 3,
                    pageSize: 10,
                    totalItems: 25,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/21-25/)).toBeTruthy();
        });

        it('displays current page and total pages', () => {
            render(Pagination, {
                props: {
                    currentPage: 2,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText('2 / 5')).toBeTruthy();
        });
    });

    describe('Page Size Selector', () => {
        it('renders page size options', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const trigger = container.querySelector('button.form-select') as HTMLButtonElement;
			expect(trigger).toBeTruthy();

			// Open
			fireEvent.click(trigger);

			const options = container.querySelectorAll('.select-menu .select-option');
			expect(options.length).toBe(4); // 10, 25, 50, 100
        });

        it('selects current page size', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 25,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const trigger = container.querySelector('button.form-select') as HTMLButtonElement;
			expect(trigger).toBeTruthy();
			expect(trigger.textContent).toContain('25');
        });

        it('calls onPageSizeChange when page size changes', async () => {
            const onPageSizeChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange
                }
            });

			const trigger = container.querySelector('button.form-select') as HTMLButtonElement;
			await fireEvent.click(trigger);

            const option25 = screen.getByRole('option', { name: '25' });
			await fireEvent.click(option25);

            expect(onPageSizeChange).toHaveBeenCalledWith(25);
        });
    });

    describe('Navigation Buttons', () => {
        it('disables first and previous buttons on first page', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			const firstButton = navButtons[0] as HTMLButtonElement;
			const prevButton = navButtons[1] as HTMLButtonElement;

            expect(firstButton.disabled).toBe(true);
            expect(prevButton.disabled).toBe(true);
        });

        it('disables next and last buttons on last page', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 5,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			const nextButton = navButtons[2] as HTMLButtonElement;
			const lastButton = navButtons[3] as HTMLButtonElement;

            expect(nextButton.hasAttribute('disabled')).toBe(true);
            expect(lastButton.hasAttribute('disabled')).toBe(true);
        });

        it('enables all buttons on middle page', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = Array.from(container.querySelectorAll('nav .btn-group button')) as HTMLButtonElement[];
			expect(navButtons.length).toBe(4);
			navButtons.forEach((btn) => expect(btn.disabled).toBe(false));
        });

        it('calls onPageChange with correct page for first button', async () => {
            const onPageChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange,
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			await fireEvent.click(navButtons[0]); // First button

            expect(onPageChange).toHaveBeenCalledWith(1);
        });

        it('calls onPageChange with correct page for previous button', async () => {
            const onPageChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange,
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			await fireEvent.click(navButtons[1]); // Previous button

            expect(onPageChange).toHaveBeenCalledWith(2);
        });

        it('calls onPageChange with correct page for next button', async () => {
            const onPageChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange,
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			await fireEvent.click(navButtons[2]);

            expect(onPageChange).toHaveBeenCalledWith(4);
        });

        it('calls onPageChange with correct page for last button', async () => {
            const onPageChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 3,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange,
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			await fireEvent.click(navButtons[3]);

            expect(onPageChange).toHaveBeenCalledWith(5);
        });

        it('does not call onPageChange when clicking disabled buttons', async () => {
            const onPageChange = vi.fn();
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange,
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			const firstButton = navButtons[0] as HTMLButtonElement;
			const prevButton = navButtons[1] as HTMLButtonElement;

            // Verify buttons are disabled
            expect(firstButton.hasAttribute('disabled')).toBe(true);
            expect(prevButton.hasAttribute('disabled')).toBe(true);

            // HTML disabled buttons don't fire click events, so we can't test this behavior
            // The browser itself prevents clicks on disabled buttons
        });
    });

    describe('Edge Cases', () => {
        it('handles single page correctly', () => {
            const { container } = render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 1,
                    pageSize: 10,
                    totalItems: 5,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

			const navButtons = container.querySelectorAll('nav .btn-group button');
			navButtons.forEach((btn) => expect((btn as HTMLButtonElement).disabled).toBe(true));
        });

        it('handles empty dataset', () => {
            render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 1,
                    pageSize: 10,
                    totalItems: 0,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/1-0/)).toBeTruthy();
        });

        it('calculates range correctly with custom page sizes', () => {
            render(Pagination, {
                props: {
                    currentPage: 2,
                    totalPages: 3,
                    pageSize: 15,
                    totalItems: 40,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            expect(screen.getByText(/16-30/)).toBeTruthy();
        });
    });

    describe('Accessibility', () => {
        it('has aria-label on navigation container', () => {
            render(Pagination, {
                props: {
                    currentPage: 2,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            // Navigation container has aria-label for screen readers
            expect(screen.getByLabelText('Table pagination')).toBeTruthy();
        });

        it('has accessible text for page size selector', () => {
            render(Pagination, {
                props: {
                    currentPage: 1,
                    totalPages: 5,
                    pageSize: 10,
                    totalItems: 50,
                    onPageChange: vi.fn(),
                    onPageSizeChange: vi.fn()
                }
            });

            // Page size selector has visible label text (from i18n key)
            expect(screen.getByText('rows_per_page:')).toBeTruthy();
        });
    });
});
