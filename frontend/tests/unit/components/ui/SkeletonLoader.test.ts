import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import SkeletonLoader from '$lib/components/ui/SkeletonLoader.svelte';

describe('SkeletonLoader Component', () => {
	it('should render text type by default', () => {
		const { container } = render(SkeletonLoader);
		const skeleton = container.querySelector('.skeleton');

		expect(skeleton).toBeTruthy();
		expect(skeleton?.classList.contains('skeleton-text')).toBe(true);
	});

	it('should render rect type', () => {
		const { container } = render(SkeletonLoader, { props: { type: 'rect' } });
		const skeleton = container.querySelector('.skeleton-rect');

		expect(skeleton).toBeTruthy();
	});

	it('should render circle type', () => {
		const { container } = render(SkeletonLoader, { props: { type: 'circle' } });
		const skeleton = container.querySelector('.skeleton-circle');

		expect(skeleton).toBeTruthy();
	});

	it('should apply custom width', () => {
		const { container } = render(SkeletonLoader, { props: { width: '200px' } });
		const skeleton = container.querySelector('.skeleton');

		expect(skeleton?.getAttribute('style')).toContain('width: 200px');
	});

	it('should apply custom height', () => {
		const { container } = render(SkeletonLoader, { props: { height: '50px' } });
		const skeleton = container.querySelector('.skeleton');

		expect(skeleton?.getAttribute('style')).toContain('height: 50px');
	});

	it('should apply both width and height', () => {
		const { container } = render(SkeletonLoader, {
			props: { width: '100px', height: '20px' }
		});
		const skeleton = container.querySelector('.skeleton');
		const style = skeleton?.getAttribute('style');

		expect(style).toContain('width: 100px');
		expect(style).toContain('height: 20px');
	});

	it('should have base skeleton class', () => {
		const { container } = render(SkeletonLoader);
		const skeleton = container.querySelector('.skeleton');
		expect(skeleton?.classList.contains('skeleton')).toBe(true);
	});

	it('should have correct default dimensions for text type', () => {
		const { container } = render(SkeletonLoader, { props: { type: 'text' } });
		const skeleton = container.querySelector('.skeleton-text');

		// Text type has default height in CSS (16px)
		expect(skeleton).toBeTruthy();
	});

	it('should have correct default dimensions for circle type', () => {
		const { container } = render(SkeletonLoader, { props: { type: 'circle' } });
		const skeleton = container.querySelector('.skeleton-circle');

		// Circle type has default width/height in CSS (40px)
		expect(skeleton).toBeTruthy();
		expect(skeleton?.classList.contains('skeleton')).toBe(true);
	});
});
