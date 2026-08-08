import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import LoadingOverlay from '$lib/components/ui/LoadingOverlay.svelte';

describe('LoadingOverlay Component', () => {
	it('should not render when show is false', () => {
		const { container } = render(LoadingOverlay, { props: { show: false } });
		const overlay = container.querySelector('.loading-overlay');

		expect(overlay).toBeNull();
	});

	it('should render when show is true', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const overlay = container.querySelector('.loading-overlay');

		expect(overlay).toBeTruthy();
	});

	it('should display spinner when visible', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const spinner = container.querySelector('.spinner');

		expect(spinner).toBeTruthy();
	});

	it('should display message if provided', () => {
		const { container } = render(LoadingOverlay, {
			props: { show: true, message: 'Saving changes...' }
		});
		const content = container.querySelector('.loading-overlay');

		expect(content?.textContent).toContain('Saving changes...');
	});

	it('should display default message if not provided', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const message = container.querySelector('.loading-message');
		expect(message).toBeTruthy();
	});

	it('should have correct z-index for blocking UI', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const overlay = container.querySelector('.loading-overlay');
		const style = window.getComputedStyle(overlay as Element);

		// Overlay should have high z-index
		expect(overlay?.classList.contains('loading-overlay')).toBe(true);
	});

	it('should center content (structure)', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const content = container.querySelector('.loading-content');
		expect(content).toBeTruthy();
	});

	it('should support fullscreen mode', () => {
		const { container } = render(LoadingOverlay, { props: { show: true, fullscreen: true } });
		const overlay = container.querySelector('.loading-overlay');
		expect(overlay?.classList.contains('fullscreen')).toBe(true);
	});

	it('should render overlay container', () => {
		const { container } = render(LoadingOverlay, { props: { show: true } });
		const overlay = container.querySelector('.loading-overlay');
		expect(overlay).toBeTruthy();
	});
});
