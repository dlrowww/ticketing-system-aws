<script lang="ts">
	import { getMessage } from '$lib/i18n';

	const currentYear = new Date().getFullYear();
</script>

<footer class="app-footer">
	<div class="footer-content">
		<div class="footer-text">
			© {currentYear}
			{getMessage('company_name')} · {getMessage('footer_all_rights_reserved')}
		</div>
		<div class="footer-version">
			{getMessage('footer_version')}: 1.0
		</div>
	</div>
</footer>

<style>
	.app-footer {
		background: var(--ironpack-white);
		border-top: 1px solid rgba(var(--ironpack-red-rgb), 0.09);
		padding: 0.7em 1.5em;
		flex-shrink: 0;
	}

	.footer-content {
		display: flex;
		align-items: center;
		justify-content: space-between;
		flex-wrap: wrap;
		gap: 0.5rem;
		font-size: 0.875rem;
		color: var(--ironpack-font-dark);
	}

	.footer-text,
	.footer-version {
		margin: 0;
	}

	@media (max-width: 576px) {
		.footer-content {
			flex-direction: column;
			text-align: center;
		}
	}
</style>
