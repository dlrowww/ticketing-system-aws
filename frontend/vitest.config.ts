import { defineConfig } from 'vitest/config';
import { svelte } from '@sveltejs/vite-plugin-svelte';
import path from 'path';

export default defineConfig({
	plugins: [svelte({ hot: !process.env.VITEST })],
	test: {
		globals: true,
		environment: 'happy-dom',
		setupFiles: ['./tests/setup.ts'],
		include: ['tests/**/*.test.ts'],
		coverage: {
			provider: 'v8',
			reporter: ['text', 'html', 'lcov'],
			exclude: [
				'node_modules/',
				'tests/',
				'src/lib/types/generated/',
				'*.config.*',
				'**/*.d.ts'
			]
		}
	},
	resolve: {
		alias: {
			$lib: path.resolve(__dirname, './src/lib'),
			$app: path.resolve(__dirname, './node_modules/@sveltejs/kit/src/runtime/app')
		}
	}
});
