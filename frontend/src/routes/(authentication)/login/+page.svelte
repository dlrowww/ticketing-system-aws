<script lang="ts">
	import { goto } from '$app/navigation';
    import { getMessage } from '$lib/i18n';
	import { API_BASE } from '$lib/config';
	import { UserRole } from '$lib/types/enums';
	import Input from '$lib/components/ui/Input.svelte';
	import PasswordInput from '$lib/components/ui/PasswordInput.svelte';
	import Button from '$lib/components/ui/Button.svelte';
	import FormError from '$lib/components/ui/FormError.svelte';

	let email = $state('');
    let password = $state('');
    let error = $state('');
    let loading = $state(false);

	async function handleLogin(e: SubmitEvent) {
		e.preventDefault();
		loading = true;
		error = '';

		try {
			const res = await fetch(`${API_BASE}/auth/login`, {
				method: 'POST',
				headers: {
					'Content-Type': 'application/json'
				},
				body: JSON.stringify({ email, password }),
				credentials: 'include'
			});
			let data, text;
			try {
				text = await res.text();
				data = JSON.parse(text);
			} catch (e) {
				console.error("Raw backend response:", text);
				error = "Login fails: backend error. See console for details.";
				return;
			}

			if (!res.ok) {
				const code = data.code || 'UNKNOWN_ERROR';
				error = getMessage(`error_code_${code}`);
			}

			// Decide landing page based on actual role.
			// Backend returns cookie-only JWT on login, so we query /auth/me.
			try {
				const meRes = await fetch(`${API_BASE}/auth/me`, { credentials: 'include' });
				if (meRes.ok) {
					const me = (await meRes.json()) as { roleId?: number | string };
					const roleId = Number(me.roleId);
					const isDashboardRole = roleId === UserRole.Admin || roleId === UserRole.TeamLeader;
					await goto(isDashboardRole ? '/app/dashboard' : '/app/my-tickets');
					return;
				}
			} catch {
				// fall through
			}

			await goto('/app/my-tickets');

		} catch (err) {
			console.error('Login fails: ' + err);
			error = 'Network error. Please try again.';
		} finally {
			loading = false;
		}
	}
</script>

<h2 class="login-title mb-4 text-center">{getMessage('login')}</h2>
<form class="login-content" onsubmit={handleLogin}>
	<div class="mb-3">
		<label for="loginEmail" class="form-label">{getMessage('email')}</label>
		<Input
			type="email"
			id="loginEmail"
			bind:value={email}
			placeholder={getMessage('email_placeholder')}
			required
			disabled={loading}
		/>
	</div>
	<div class="mb-3">
		<label for="loginPassword" class="form-label">{getMessage('password')}</label>
		<PasswordInput
			id="loginPassword"
			bind:value={password}
			placeholder={getMessage('password_placeholder')}
			autocomplete="current-password"
			required
			disabled={loading}
		/>
	</div>
	<FormError message={error} />
	<div class="login-actions">
		<div class="d-grid mb-3">
			<Button type="submit" variant="primary" size="lg" loading={loading} disabled={loading}>
				{getMessage('login')}
			</Button>
		</div>
	</div>
</form>