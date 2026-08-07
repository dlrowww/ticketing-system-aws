import devtoolsJson from 'vite-plugin-devtools-json';
import { sveltekit } from '@sveltejs/kit/vite';
import { defineConfig, loadEnv } from 'vite';

export default defineConfig(({ mode }) => {
	const env = loadEnv(mode, process.cwd(), '');
	const backend = env.LOOKUPS_API || 'http://localhost:5192';

	return {
		plugins: [sveltekit(), devtoolsJson()],
		server: {
			proxy: {
				// Any browser request to /api/* will be proxied to the backend
				'/api': {
					target: backend,
					changeOrigin: true,
					// Allow SvelteKit endpoints under /api to be handled by SvelteKit.
					// Without this, Vite forwards /api/set-locale to the backend (404).
					bypass: (req) => {
						const url = req.url ?? '';
						if (url === '/api/set-locale' || url.startsWith('/api/set-locale?')) {
							return url;
						}

						return undefined;
					}
				}
			}
		}
	};
});
