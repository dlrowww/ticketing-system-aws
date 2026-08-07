import { get } from 'svelte/store';
import {
	register,
	init as _init,
	waitLocale as _waitLocale,
	format,
	//getLocaleFromNavigator,
	t,
	locale as _locale
} from 'svelte-i18n';

const defaultLocale = 'en-US';
let currentLocale: string | undefined;
let readyPromise: Promise<void> | null = null;

// Register languages
register(defaultLocale, () => import('./locales/en.json'));
register('pl-PL', () => import('./locales/pl.json'));
// register('en', () => import('./locales/en.json'));
// register('pl', () => import('./locales/pl.json'));

// init({
//     fallbackLocale: defaultLocale,
//     //initialLocale: browser ? window.navigator.language : defaultLocale, //getLocaleFromNavigator()
// });

_locale.subscribe((value) => {
	console.error('locale changed = ' + value);
	currentLocale = value || undefined;
	readyPromise = null;
});

function ensureReady() {
	if (!readyPromise) readyPromise = _waitLocale();
	return readyPromise;
}

// Call this once at app start (layout) with the SSR-provided locale
export function init(opts: { initialLocale?: string; fallbackLocale?: string } = {}) {
	_init({
		fallbackLocale: opts.fallbackLocale ?? defaultLocale,
		initialLocale: opts.initialLocale ?? defaultLocale
	});
}

export function getMessage(key: string, values?: Record<string, any>): string {
	const translate = get(t);
	// translate(key) will return the key itself if not ready,
	// which is why layouts must await waitLocale() before rendering.
	return translate ? translate(key, values ? { values } : undefined) : key;
}

// export function getMessage(key: string, values?: Record<string, any>): string {
//     let value = '';
//     console.error('getMessage > locale = ' + currentLocale);
//     t.subscribe(fn => { value = fn(key, values); })();
//     console.error('getMessage > value = ' + value);
//     return value;
// }

// function getStoreValue(key: string, paramsObj?: Record<string, any>): string {
//     console.error('getStoreValue > locale = ' + currentLocale);
// 	let s = get(format);
// 	let aValue = s(`${key}`, {locale: currentLocale});
//     console.error('getStoreValue > value = ' + aValue);
// 	return paramsObj ? paramsToString(aValue, paramsObj) : aValue;
// }

// export function getMessage (key: string, paramsObj?: Record<string, any>): string {
//     console.error('getMessage > key = ' + key);
// 	if (key) {
// 		return getStoreValue(key, paramsObj);
// 	}
// 	return '';
// };

// export function getLocale(): string {
//   let value = '';
//   locale.subscribe(l => { value = l; })();
//   return value;
// }

export function setLocale(inLocale: string | undefined) {
	if (inLocale && inLocale !== currentLocale) {
		_locale.set(inLocale);
		readyPromise = null;
	}
}

// For layouts to await before rendering
export function waitLocale() {
	return ensureReady();
}

export default {
	getMessage,
	init,
	setLocale,
	waitLocale
};
