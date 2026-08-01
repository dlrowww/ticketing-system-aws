<script lang="ts">
	import type { CategoryQuery } from '$lib/types/categories';

	import { getMessage } from '$lib/i18n';
	import CategoriesTable from '$lib/components/tables/CategoriesTable.svelte';

	let { data }: { data: { initial: any; query: CategoryQuery; error?: string } } = $props();
</script>

<section class="container py-3">
	<h1 class="h4 mb-3">{getMessage('admin_categories_title')}</h1>
	{#if data?.error}
		<div class="alert alert-warning">{data.error}</div>
	{/if}

	<CategoriesTable initial={data.initial} initialQuery={data.query} />
</section>
