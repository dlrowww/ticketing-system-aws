import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import FormError from '$lib/components/ui/FormError.svelte';

describe('FormError Component', () => {
	it('should render error message', () => {
		const { container } = render(FormError, { 
			props: { message: 'This field is required' } 
		});
		const alert = container.querySelector('.form-error[role="alert"]');
		
		expect(alert).toBeTruthy();
		expect(alert?.textContent).toContain('This field is required');
	});

	it('should have error icon', () => {
		const { container } = render(FormError, { 
			props: { message: 'Error' } 
		});
		const icon = container.querySelector('i.bi.bi-exclamation-circle-fill');
		
		expect(icon).toBeTruthy();
	});

	it('should have correct ARIA attributes', () => {
		const { container } = render(FormError, { 
			props: { message: 'Error' } 
		});
		const alert = container.querySelector('.form-error');
		
		expect(alert?.getAttribute('role')).toBe('alert');
	});

	it('should render multiple errors as a list', () => {
		const { container } = render(FormError, {
			props: { errors: ['Error 1', 'Error 2'] }
		});

		const alert = container.querySelector('.form-error[role="alert"]');
		expect(alert).toBeTruthy();

		const items = container.querySelectorAll('ul li');
		expect(items.length).toBe(2);
		expect(items[0]?.textContent).toContain('Error 1');
		expect(items[1]?.textContent).toContain('Error 2');
	});

	it('should not render if message is empty', () => {
		const { container } = render(FormError, { props: { message: '' } });
		const alert = container.querySelector('.form-error');
		
		expect(alert).toBeNull();
	});

	it('should not render if message is undefined', () => {
		const { container } = render(FormError);
		const alert = container.querySelector('.form-error');
		
		expect(alert).toBeNull();
	});

	it('should render long error messages', () => {
		const longMessage = 'This is a very long error message that should still be displayed correctly without breaking the layout or causing any visual issues';
		const { container } = render(FormError, { props: { message: longMessage } });
		const alert = container.querySelector('.form-error');
		
		expect(alert?.textContent).toContain(longMessage);
	});

	it('should handle HTML entities in message', () => {
		const message = 'Error: <script>alert("test")</script>';
		const { container } = render(FormError, { props: { message } });
		const alert = container.querySelector('.form-error');
		
		// Should escape HTML and show as text
		expect(alert?.textContent).toContain(message);
		expect(container.querySelector('script')).toBeNull();
	});
});
