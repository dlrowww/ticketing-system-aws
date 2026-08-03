import { render } from '@testing-library/svelte';
import { describe, it, expect } from 'vitest';

import ChartCanvas from '$lib/components/charts/ChartCanvas.svelte';

describe('ChartCanvas', () => {
	it('renders a canvas with aria-label', () => {
		const { container } = render(ChartCanvas, {
			props: {
				ariaLabel: 'chart-label',
				config: {
					type: 'bar',
					data: { labels: ['A'], datasets: [{ data: [1] }] }
				} as any
			}
		});

		const canvas = container.querySelector('canvas');
		expect(canvas).toBeInTheDocument();
		expect(canvas).toHaveAttribute('aria-label', 'chart-label');
	});
});
