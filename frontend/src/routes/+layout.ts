import type { LayoutLoad } from './$types';

import { browser } from '$app/environment';
import '$lib/i18n';
import { setLocale, waitLocale, init } from '$lib/i18n';

export const load: LayoutLoad = async ({ data }) => {
	const { locale } = data;

	init({
		fallbackLocale: 'en-US',
		initialLocale: locale
	});
	if (browser && locale) {
		setLocale(locale);
	}
	await waitLocale();
	return {
		...data
	};
};
