import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import Button from '$lib/components/ui/Button.svelte';

describe('Button Component - Loading State Enhancement', () => {
	it('should render button with default props', () => {
		const { container } = render(Button, { props: { label: 'Click me' } });
		const button = container.querySelector('button');
		
		expect(button).toBeTruthy();
		expect(button?.textContent).toContain('Click me');
	});

	it('should not show spinner when loading is false', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: false } 
		});
		const spinner = container.querySelector('.spinner');
		
		expect(spinner).toBeNull();
	});

	it('should show spinner when loading is true', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: true } 
		});
		const spinner = container.querySelector('.spinner');
		
		expect(spinner).toBeTruthy();
	});

	it('should disable button when loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: true } 
		});
		const button = container.querySelector('button');
		
		expect(button?.hasAttribute('disabled')).toBe(true);
	});

	it('should not disable button when not loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: false } 
		});
		const button = container.querySelector('button');
		
		expect(button?.hasAttribute('disabled')).toBe(false);
	});

	it('should preserve button text when loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Saving...', loading: true } 
		});
		const button = container.querySelector('button');
		
		expect(button?.textContent).toContain('Saving...');
	});

	it('should render small spinner for loading state', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: true } 
		});
		const spinner = container.querySelector('.spinner-sm');
		
		expect(spinner).toBeTruthy();
	});

	it('should add spacing between spinner and text', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', loading: true } 
		});
		const spinnerWrap = container.querySelector('.btn-spinner');
		
		expect(spinnerWrap).toBeTruthy();
	});

	it('should preserve variant when loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Delete', variant: 'danger', loading: true } 
		});
		const button = container.querySelector('button');
		
		expect(button?.classList.contains('btn-danger')).toBe(true);
	});

	it('should preserve size when loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', size: 'sm', loading: true } 
		});
		const button = container.querySelector('button');
		
		expect(button?.classList.contains('btn-sm')).toBe(true);
	});

	it('should work with type prop', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', type: 'submit', loading: true } 
		});
		const button = container.querySelector('button');
		
		expect(button?.getAttribute('type')).toBe('submit');
	});

	it('should respect explicit disabled prop even when not loading', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', disabled: true, loading: false } 
		});
		const button = container.querySelector('button');
		
		expect(button?.hasAttribute('disabled')).toBe(true);
	});

	it('should handle both disabled and loading states', () => {
		const { container } = render(Button, { 
			props: { label: 'Submit', disabled: true, loading: true } 
		});
		const button = container.querySelector('button');
		const spinner = container.querySelector('.spinner');
		
		expect(button?.hasAttribute('disabled')).toBe(true);
		expect(spinner).toBeTruthy();
	});
});
