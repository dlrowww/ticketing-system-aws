<script lang="ts">
	import { onDestroy, onMount } from 'svelte';
	import type { ChartConfiguration } from 'chart.js';

	type Props = {
		config: ChartConfiguration;
		ariaLabel: string;
		class?: string;
	};

	let { config, ariaLabel, class: className }: Props = $props();

	let canvasEl: HTMLCanvasElement | null = null;
	let chart: { destroy: () => void; update?: () => void } | null = null;
	let mounted = false;

	async function renderChart() {
		if (!mounted) {
			return;
		}
		if (!canvasEl) {
			return;
		}
		const ctx = canvasEl.getContext('2d');
		if (!ctx) {
			return;
		}

		const mod = await import('chart.js/auto');
		const Chart = (mod as any).default;

		chart?.destroy();
		chart = new Chart(ctx, config);
	}

	onMount(() => {
		mounted = true;
		void renderChart();
	});

	$effect(() => {
		if (typeof window === 'undefined') {
			return;
		}
		void renderChart();
	});

	onDestroy(() => {
		chart?.destroy();
		chart = null;
	});
</script>

<div class={['chart-canvas', className].filter(Boolean).join(' ')}>
	<canvas bind:this={canvasEl} aria-label={ariaLabel}></canvas>
</div>

<style>
	.chart-canvas {
		width: 100%;
		height: 100%;
		overflow: hidden;
	}

	.chart-canvas canvas {
		display: block;
		width: 100% !important;
		height: 100% !important;
	}
</style>
