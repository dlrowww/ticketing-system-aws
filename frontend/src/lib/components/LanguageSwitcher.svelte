<script lang="ts">
	import { invalidateAll } from '$app/navigation';
	import Select from '$lib/components/ui/Select.svelte';
	import ResourceProvider from '$lib/i18n';

	let { locale }: { locale: string } = $props();

	let selectedLocale = $state(locale);

	// Watch for locale prop changes (e.g., from SSR)
	$effect(() => {
		selectedLocale = locale;
	});

	// Watch for locale changes from Select component
	$effect(() => {
		if (selectedLocale && selectedLocale !== locale) {
			changeLang(selectedLocale);
		}
	});

	async function changeLang(newLocale: string) {
		if (newLocale) {
			ResourceProvider.setLocale(newLocale);
			await fetch('/api/set-locale', {
				method: 'POST',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({ locale: newLocale })
			});
			await ResourceProvider.waitLocale();
			// Force SvelteKit to refresh SSR-provided data and re-render
			await invalidateAll();
		}
	}
</script>

<div class="lang-select">
	<Select
		bind:value={selectedLocale}
		icon="bi bi-globe"
		options={[
			{ value: 'pl-PL', label: 'PL' },
			{ value: 'en-US', label: 'EN' }
		]}
	/>
</div>

<style>
	.lang-select {
		display: inline-block;
	}

	:global(.lang-select .form-select) {
		cursor: pointer;
	}

	:global(.lang-select .form-select:hover) {
		background-color: rgba(var(--bs-secondary-rgb), 0.16);
	}
</style>
