import { describe, it, expect, vi } from 'vitest';
import { render, fireEvent } from '@testing-library/svelte';
import UserSelect from '$lib/components/ui/UserSelect.svelte';
import type { AssignableUserDto } from '$lib/types/users';

// Mock getMessage to return the key
vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

describe('UserSelect Component - Assignment Feature', () => {
	const mockUsers: AssignableUserDto[] = [
		{
			userId: 1,
			name: 'Jan Kowalski',
			email: 'jan@ironpack.pl',
			roleName: 'Support',
			categoryId: 1,
			categoryNamePl: 'IT',
			categoryNameEn: 'IT'
		},
		{
			userId: 2,
			name: 'Anna Nowak',
			email: 'anna@ironpack.pl',
			roleName: 'TeamLeader',
			categoryId: 1,
			categoryNamePl: 'IT',
			categoryNameEn: 'IT'
		},
		{
			userId: 3,
			name: 'Marek Zieliński',
			email: 'marek@ironpack.pl',
			roleName: 'Admin',
			categoryId: null,
			categoryNamePl: null,
			categoryNameEn: null
		}
	];

	function getSelectButton(container: HTMLElement): HTMLButtonElement {
		const btn = container.querySelector(
			'.select-container > button.form-select:not(.select-measure)'
		) as HTMLButtonElement | null;
		expect(btn).not.toBeNull();
		return btn!;
	}

	async function openMenu(container: HTMLElement) {
		const btn = getSelectButton(container);
		await fireEvent.click(btn);
	}

	function getMenuOptions(container: HTMLElement): HTMLButtonElement[] {
		return Array.from(
			container.querySelectorAll('.select-menu .select-option')
		) as HTMLButtonElement[];
	}

	it('renders with users list', async () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: mockUsers,
				loading: false,
				disabled: false,
				id: 'assignee'
			}
		});

		const btn = getSelectButton(container);
		expect(btn).toHaveAttribute('id', 'assignee');
		expect(btn).toBeEnabled();
		// Placeholder shown when value is null
		expect(btn.textContent).toContain('assigned_to');

		await openMenu(container);
		const options = getMenuOptions(container).map((o) => o.textContent ?? '');
		// Includes placeholder option + "Unassigned" option + 3 users
		expect(options.some((t) => t.includes('assigned_to'))).toBe(true);
		expect(options.some((t) => t.includes('— not_assigned —'))).toBe(true);
		expect(options.some((t) => t.includes('Jan Kowalski (Support)'))).toBe(true);
		expect(options.some((t) => t.includes('Anna Nowak (TeamLeader)'))).toBe(true);
		expect(options.some((t) => t.includes('Marek Zieliński (Admin)'))).toBe(true);
	});

	it('renders loading state', () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: [],
				loading: true
			}
		});

		const btn = getSelectButton(container);
		expect(btn).toBeDisabled();
		expect(btn.textContent).toContain('loading');
	});

	it('renders empty state when no assignable users', () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: [],
				loading: false
			}
		});

		const btn = getSelectButton(container);
		expect(btn).toBeDisabled();
		expect(btn.textContent).toContain('no_assignable_users');
	});

	it('emits change event on selection and updates value', async () => {
		let selectedValue: number | null = null;

		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				get value() {
					return selectedValue;
				},
				set value(v: number | null) {
					selectedValue = v;
				},
				users: mockUsers
			}
		});

		await openMenu(container);
		let options = getMenuOptions(container);
		const jan = options.find((o) => (o.textContent ?? '').includes('Jan Kowalski (Support)'));
		expect(jan).not.toBeUndefined();
		await fireEvent.click(jan!);

		expect(selectedValue).toBe(1);

		await openMenu(container);
		options = getMenuOptions(container);
		const anna = options.find((o) => (o.textContent ?? '').includes('Anna Nowak (TeamLeader)'));
		expect(anna).not.toBeUndefined();
		await fireEvent.click(anna!);

		expect(selectedValue).toBe(2);

		await openMenu(container);
		options = getMenuOptions(container);
		const unassign = options.find((o) => (o.textContent ?? '').includes('— not_assigned —'));
		expect(unassign).not.toBeUndefined();
		await fireEvent.click(unassign!);

		expect(selectedValue).toBeNull();
	});

	it('supports unassigned option when not required', async () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: mockUsers,
				required: false
			}
		});

		await openMenu(container);
		const options = getMenuOptions(container).map((o) => o.textContent ?? '');
		expect(options.some((t) => t.includes('— not_assigned —'))).toBe(true);
	});

	it('does not show unassigned option when required', async () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: 1,
				users: mockUsers,
				required: true
			}
		});

		// Selected user should be displayed, but excluded from the dropdown options.
		expect(getSelectButton(container).textContent).toContain('Jan Kowalski');

		await openMenu(container);
		const options = getMenuOptions(container).map((o) => o.textContent ?? '');
		expect(options.some((t) => t.includes('— not_assigned —'))).toBe(false);
		expect(options.some((t) => t.includes('Jan Kowalski'))).toBe(false);
		expect(options.some((t) => t.includes('Anna Nowak'))).toBe(true);
		expect(options.some((t) => t.includes('Marek Zieliński'))).toBe(true);
	});

	it('sets selected value correctly', () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: 2,
				users: mockUsers
			}
		});

		const btn = getSelectButton(container);
		expect(btn.textContent).toContain('Anna Nowak (TeamLeader)');
	});

	it('respects disabled state', () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: mockUsers,
				disabled: true
			}
		});

		const btn = getSelectButton(container);
		expect(btn).toBeDisabled();
	});

	it('has proper id for label association', () => {
		const { container } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: null,
				users: mockUsers,
				id: 'assignee'
			}
		});

		const btn = getSelectButton(container);
		expect(btn).toHaveAttribute('id', 'assignee');
	});

	it('preserves selected value when users list updates', async () => {
		const { container, rerender } = render(UserSelect, {
			props: {
				ticketId: 1,
				value: 2,
				users: mockUsers
			}
		});

		let btn = getSelectButton(container);
		expect(btn.textContent).toContain('Anna Nowak (TeamLeader)');

		// Update users list (add one more user)
		const updatedUsers: AssignableUserDto[] = [
			...mockUsers,
			{
				userId: 4,
				name: 'Ewa Kowalczyk',
				email: 'ewa@ironpack.pl',
				roleName: 'Support',
				categoryId: 1,
				categoryNamePl: 'IT',
				categoryNameEn: 'IT'
			}
		];

		// Use rerender instead of $set for Svelte 5 compatibility
		await rerender({ users: updatedUsers });

		btn = getSelectButton(container);
		// Selected value should remain the same
		expect(btn.textContent).toContain('Anna Nowak (TeamLeader)');
	});
});
