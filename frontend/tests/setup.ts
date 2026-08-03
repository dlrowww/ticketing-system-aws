import '@testing-library/jest-dom/vitest';
import { vi } from 'vitest';
import { addMessages, init } from 'svelte-i18n';
import en from '../src/lib/i18n/locales/en.json';

addMessages('en-US', en);
init({
	fallbackLocale: 'en-US',
	initialLocale: 'en-US'
});

vi.mock('$env/dynamic/public', () => ({
	env: {
		PUBLIC_API_BASE: '/api',
		PUBLIC_DEFAULT_LOCALE: 'en-US'
	}
}));

// Mock SvelteKit modules
vi.mock('$app/environment', () => ({
	browser: false,
	dev: true,
	building: false,
	version: 'test'
}));

vi.mock('$app/navigation', () => ({
	goto: vi.fn(),
	invalidate: vi.fn(),
	invalidateAll: vi.fn(),
	preloadData: vi.fn(),
	preloadCode: vi.fn(),
	beforeNavigate: vi.fn(),
	afterNavigate: vi.fn(),
	pushState: vi.fn(),
	replaceState: vi.fn()
}));

vi.mock('$app/stores', () => {
	const createStore = <T>(value: T) => ({
		subscribe(run: (current: T) => void) {
			run(value);
			return () => undefined;
		}
	});
	const pageValue = {
		data: { user: null },
		url: new URL('http://localhost/')
	};
	const page = createStore(pageValue);
	const navigating = createStore(null);
	const updated = { ...createStore(false), check: vi.fn() };

	const getStores = () => {
		return { navigating, page, updated };
	};

	return { getStores, navigating, page, updated };
});
