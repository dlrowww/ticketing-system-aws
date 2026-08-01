import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import Spinner from '$lib/components/ui/Spinner.svelte';

describe('Spinner Component', () => {
	it('should render with default props', () => {
		const { container } = render(Spinner);
		const spinner = container.querySelector('.spinner');
		
		expect(spinner).toBeTruthy();
		expect(spinner?.classList.contains('spinner-md')).toBe(true);
		expect(spinner?.classList.contains('spinner-primary')).toBe(true);
		expect(spinner?.getAttribute('role')).toBe('status');
	});

	it('should render small size', () => {
		const { container } = render(Spinner, { props: { size: 'sm' } });
		const spinner = container.querySelector('.spinner.spinner-sm');
		
		expect(spinner).toBeTruthy();
	});

	it('should render large size', () => {
		const { container } = render(Spinner, { props: { size: 'lg' } });
		const spinner = container.querySelector('.spinner.spinner-lg');
		expect(spinner).toBeTruthy();
	});

	it('should render medium size (default)', () => {
		const { container } = render(Spinner, { props: { size: 'md' } });
		const spinner = container.querySelector('.spinner');
		
		expect(spinner?.classList.contains('spinner-md')).toBe(true);
	});

	it('should render with secondary variant', () => {
		const { container } = render(Spinner, { props: { variant: 'secondary' } });
		const spinner = container.querySelector('.spinner');
		
		expect(spinner?.classList.contains('spinner-secondary')).toBe(true);
	});

	it('should render with primary variant (default)', () => {
		const { container } = render(Spinner, { props: { variant: 'primary' } });
		const spinner = container.querySelector('.spinner');
		
		expect(spinner?.classList.contains('spinner-primary')).toBe(true);
	});

	it('should have accessible label', () => {
		const { container } = render(Spinner);
		const srOnly = container.querySelector('.visually-hidden');
		
		expect(srOnly?.textContent).toBe('Loading...');
	});

	it('should have correct ARIA attributes', () => {
		const { container } = render(Spinner);
		const spinner = container.querySelector('.spinner');
		
		expect(spinner?.getAttribute('role')).toBe('status');
		expect(spinner?.getAttribute('aria-label')).toBe('Loading...');
	});
});
