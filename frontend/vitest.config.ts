import { defineConfig } from 'vitest/config';
import { sveltekit } from '@sveltejs/kit/vite';

export default defineConfig({
	plugins: [sveltekit()],
	test: {
		globals: true,
		setupFiles: ['./tests/setup.ts'],
		projects: [
			{
				extends: true,
				test: {
					name: 'node',
					environment: 'node',
					include: [
						'tests/unit/helpers/**/*.test.ts',
						'tests/unit/services/**/*.test.ts',
						'tests/unit/stores/**/*.test.ts',
						'tests/unit/utils/**/*.test.ts',
						'tests/unit/routes/**/*Server.test.ts',
						'tests/unit/routes/RootRedirects.test.ts',
						'tests/integration/workflows/{api-set-locale,category-management,m24-dashboard-charts,m24-dashboard-reports,m8-role-based-views,mXX-admin-users}.test.ts'
					]
				}
			},
			{
				extends: true,
				resolve: {
					conditions: ['browser']
				},
				test: {
					name: 'components',
					environment: 'happy-dom',
					include: [
						'tests/unit/components/**/*.test.ts',
						'tests/unit/routes/DashboardPage.test.ts',
						'tests/unit/routes/DashboardChartsPage.test.ts',
						'tests/unit/routes/DashboardPage.filtersSummary.test.ts',
						'tests/integration/workflows/ticket-assignment.test.ts'
					]
				}
			}
		],
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
	}
});
