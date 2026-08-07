import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';
import Badge from '$lib/components/ui/Badge.svelte';

describe('Badge', () => {
	it('renders with default props (primary variant, md size)', () => {
		const { container } = render(Badge, { props: { label: 'Test Badge' } });
		const badge = container.querySelector('.badge');

		expect(badge).toBeInTheDocument();
		expect(badge).toHaveClass('bg-primary');
		expect(badge).toHaveClass('badge-md');
		expect(badge).toHaveTextContent('Test Badge');
	});

	it('renders with secondary variant', () => {
		const { container } = render(Badge, { props: { variant: 'secondary', label: 'Secondary' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-secondary');
		expect(badge).toHaveTextContent('Secondary');
	});

	it('renders with success variant', () => {
		const { container } = render(Badge, { props: { variant: 'success', label: 'Success' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-success');
	});

	it('renders with danger variant', () => {
		const { container } = render(Badge, { props: { variant: 'danger', label: 'Danger' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-danger');
	});

	it('renders with warning variant', () => {
		const { container } = render(Badge, { props: { variant: 'warning', label: 'Warning' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-warning');
	});

	it('renders with info variant', () => {
		const { container } = render(Badge, { props: { variant: 'info', label: 'Info' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-info');
	});

	it('renders with light variant', () => {
		const { container } = render(Badge, { props: { variant: 'light', label: 'Light' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-light');
	});

	it('renders with dark variant', () => {
		const { container } = render(Badge, { props: { variant: 'dark', label: 'Dark' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-dark');
	});

	it('renders with custom variant (no default bg class)', () => {
		const { container } = render(Badge, {
			props: { variant: 'custom', class: 'bg-custom-color', label: 'Custom' }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('bg-custom-color');
		expect(badge).not.toHaveClass('bg-custom');
	});

	it('renders with small size', () => {
		const { container } = render(Badge, { props: { size: 'sm', label: 'Small' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('badge-sm');
	});

	it('renders with large size', () => {
		const { container } = render(Badge, { props: { size: 'lg', label: 'Large' } });
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('badge-lg');
	});

	it('accepts custom class names', () => {
		const { container } = render(Badge, {
			props: { class: 'my-custom-class', label: 'Custom Class' }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toHaveClass('my-custom-class');
		expect(badge).toHaveClass('badge');
		expect(badge).toHaveClass('bg-primary');
	});

	it('renders with aria-label for accessibility', () => {
		const { container } = render(Badge, {
			props: { ariaLabel: 'Status: Active', label: 'Active' }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toHaveAttribute('aria-label', 'Status: Active');
	});

	it('renders without aria-label when not provided', () => {
		const { container } = render(Badge, { props: { label: 'No Label' } });
		const badge = container.querySelector('.badge');

		expect(badge).not.toHaveAttribute('aria-label');
	});
});
