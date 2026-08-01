<script lang="ts">
	import type { LayoutData } from './$types';
	import { TopBar, NavRail, FloatingActionButton, Footer } from '$lib/components/layout';

	let { children, data }: { children: any; data: LayoutData } = $props();
</script>

<div class="app-container">
	<TopBar user={data.user} />
	
	<div class="app-body">
		<NavRail user={data.user} />
		
		<main class="app-content">
			{@render children()}
		</main>
	</div>
	
	<Footer />
	<FloatingActionButton />
</div>

<style>
	.app-container {
		height: 100vh;
		display: flex;
		flex-direction: column;
		overflow: hidden;
	}

	.app-body {
		flex: 1;
		display: flex;
		min-height: 0;
		overflow: hidden;
	}

	.app-content {
		flex: 1;
		overflow-y: auto;
		background: var(--ironpack-white);
		border-left: 1px solid rgba(var(--ironpack-red-rgb), 0.09);
		padding: 1rem 1.5rem;
	}

	@media (max-width: 767px) {
		.app-body {
			flex-direction: column;
		}
	}
</style>
