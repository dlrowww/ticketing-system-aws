<script lang="ts">
	import type { UserQuery } from '$lib/types/users';

	import { getMessage } from '$lib/i18n';
	import UsersTable from '$lib/components/tables/UsersTable.svelte';

	let { data }: { data: { initial: any; query: UserQuery; error?: string } } = $props();
</script>

<section class="container py-3">
	<h1 class="h4 mb-3">{getMessage('admin_users_title')}</h1>
	{#if data?.error}
		<div class="alert alert-warning">{data.error}</div>
	{/if}

	<UsersTable initial={data.initial} initialQuery={data.query} />
</section>
