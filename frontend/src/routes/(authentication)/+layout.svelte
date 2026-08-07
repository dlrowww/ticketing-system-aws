<script lang="ts">
	import type { LayoutData } from './$types';

	import LanguageSwitcher from '$lib/components/LanguageSwitcher.svelte';
	import ResourceProvider from '$lib/i18n';
	import { DEFAULT_LOCALE } from '$lib/config';

	// let { data = {
	// 	user: undefined,
	// 	locale: undefined,
	// 	lookups
	// }, children } = $props();

	let { data, children }: { data: LayoutData; children: any } = $props();

	let { locale } = data;

	let currentLocale = $derived(locale ?? DEFAULT_LOCALE);
</script>

<!-- {#await loading}
...
{:then} -->
<div class="ironpack-login-bg">
	<div class="ironpack-login-card shadow">
		<LanguageSwitcher locale={currentLocale} />
		<div class="login-logo-bar">
			<img src="/ticketing-system-logo.svg" alt="IronPack Ticketing System" class="logo" />
		</div>
		{@render children()}
	</div>
</div>
<!-- {:catch e}
  <h1>ERROR loading translations: {e?.message ?? e}</h1>
{/await} -->
