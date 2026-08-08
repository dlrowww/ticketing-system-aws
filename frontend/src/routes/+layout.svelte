<script lang="ts">
	import type { LayoutData } from './$types';
	import { setLookups, getLookups } from '$lib/lookups/Lookups';
	import { categories } from '$lib/stores/categories';
	// export let data: { lookups: import('$lib/lookups/Lookups').LookupsPayload };
	import '../styles/main.scss';
	import 'bootstrap-icons/font/bootstrap-icons.css';
	import { browser } from '$app/environment';
	import { onMount } from 'svelte';
	import ResourceProvider from '$lib/i18n';
	import { DEFAULT_LOCALE } from '$lib/config';
	import ToastContainer from '$lib/components/feedback/ToastContainer.svelte';
	import ModalManager from '$lib/components/modals/base/ModalManager.svelte';

	let { children, data }: { children: any; data: LayoutData } = $props();
	let activeLocale = $state(data?.locale ?? DEFAULT_LOCALE);
	let localeKey = $state(0);

	// Initialize i18n using the SSR cookie-driven locale
	ResourceProvider.init({
		initialLocale: data?.locale ?? DEFAULT_LOCALE,
		fallbackLocale: DEFAULT_LOCALE
	});

	// Wait until dictionaries for that locale are loaded
	let loading = $state(ResourceProvider.waitLocale());

	// When the SSR-provided locale changes (e.g., after /api/set-locale + invalidateAll),
	// update i18n and remount the subtree so getMessage()-based strings re-render.
	$effect(() => {
		const nextLocale = data?.locale ?? DEFAULT_LOCALE;
		if (nextLocale === activeLocale) return;

		activeLocale = nextLocale;
		ResourceProvider.setLocale(nextLocale);
		loading = ResourceProvider.waitLocale();
		localeKey += 1;
	});

	// Hydrate the store once the root layout is created on the client.
	// If the server already gave us the same version, we skip.
	const current = getLookups();

	onMount(() => {
		if (!current || current.version !== data.lookups.version) {
			setLookups(data.lookups);
		}
		// Hydrate category store with SSR data
		if (data.categories) {
			categories.set(data.categories);
		}
		if (browser) import('bootstrap');
	});
</script>

{#await loading}
	<h1>Loading...</h1>
{:then}
	{#key localeKey}
		{@render children()}
	{/key}
{:catch error}
	<h1>ERROR loading resources: {error?.message ?? error}</h1>
{/await}

<!-- Global Toast Notifications -->
<ToastContainer />

<!-- Global Modal Manager -->
<ModalManager />
