<script>
	import { goto } from '$app/navigation';
	import { getMessage } from '$lib/i18n';

	function goHome() {
		goto('/app');
	}
	function goLogin() {
		goto('/login');
	}
</script>

<div class="unauth-container">
	<div class="unauth-card">
		<svg class="unauth-icon" width="56" height="56" viewBox="0 0 56 56" fill="none">
			<circle cx="28" cy="28" r="28" fill="#b32c2c" />
			<path d="M18 38L38 18M18 18l20 20" stroke="#fff" stroke-width="3" stroke-linecap="round" />
		</svg>
		<h1>{getMessage('access_denied')}</h1>
		<p>
			Nie masz uprawnień do wyświetlenia tej strony.<br />
			Skontaktuj się z administratorem, jeśli uważasz, że to błąd.
		</p>
		<div class="unauth-actions">
			<button onclick={goHome}>Strona główna</button>
			<button class="secondary" onclick={goLogin}>Zaloguj się</button>
		</div>
	</div>
</div>

<style>
	.unauth-container {
		min-height: 100vh;
		display: flex;
		align-items: center;
		justify-content: center;
		background: #fff;
	}

	.unauth-card {
		background: #fff;
		border: 1.5px solid #b32c2c;
		border-radius: 1.25rem;
		box-shadow: 0 6px 36px 0 rgba(60, 24, 24, 0.08);
		max-width: 410px;
		padding: 2.5rem 2rem;
		text-align: center;
	}

	.unauth-icon {
		margin-bottom: 0.6rem;
		display: block;
		margin-left: auto;
		margin-right: auto;
	}

	h1 {
		color: #b32c2c;
		font-size: 2rem;
		margin-bottom: 0.3em;
	}

	p {
		color: #181818;
		margin-bottom: 1.5em;
		font-size: 1.1rem;
	}

	.unauth-actions {
		display: flex;
		gap: 1em;
		justify-content: center;
	}

	.unauth-actions button {
		background: #b32c2c;
		color: #fff;
		border: none;
		padding: 0.65em 1.7em;
		border-radius: 1.1em;
		font-size: 1rem;
		cursor: pointer;
		transition: background 0.15s;
		font-weight: 600;
		box-shadow: 0 2px 10px 0 rgba(179, 44, 44, 0.07);
	}

	.unauth-actions button.secondary {
		background: #fff;
		color: #b32c2c;
		border: 1.5px solid #b32c2c;
	}

	.unauth-actions button:hover,
	.unauth-actions button.secondary:hover {
		background: #181818;
		color: #fff;
		border-color: #181818;
	}
</style>
