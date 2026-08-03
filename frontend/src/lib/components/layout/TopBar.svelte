<script lang="ts">
	import { goto } from '$app/navigation';
	import { page } from '$app/stores';
	import { getMessage } from '$lib/i18n';
	import { Badge } from '$lib/components/ui';
	import type { UserRole } from '$lib/types/enums';
	import { UserRole as UserRoleEnum, UserRoleKey } from '$lib/types/enums';
	import LanguageSwitcher from '$lib/components/LanguageSwitcher.svelte';

	interface User {
		id: string;
		name: string;
		email: string;
		roleId: string;
		categoryId?: string;
	}

	let { user }: { user: User } = $props();

	let showUserMenu = $state(false);
	let currentLocale = $derived($page.data.locale || 'en-US');
	const userRole = $derived(Number(user.roleId) as UserRoleEnum);

	const logoHref = $derived(
		userRole === UserRoleEnum.Admin || userRole === UserRoleEnum.TeamLeader
			? '/app/dashboard'
			: userRole === UserRoleEnum.Support
				? '/app/assigned'
				: '/app/my-tickets'
	);

	function toggleUserMenu() {
		showUserMenu = !showUserMenu;
	}

	async function handleLogout() {
		try {
			await fetch('/api/auth/logout', {
				method: 'POST',
				credentials: 'include'
			});
			goto('/login');
		} catch (error) {
			console.error('Logout failed:', error);
		}
	}

	// Close menu when clicking outside
	function handleClickOutside(event: MouseEvent) {
		const target = event.target as HTMLElement;
		if (!target.closest('.user-menu-container')) {
			showUserMenu = false;
		}
	}

	$effect(() => {
		if (showUserMenu) {
			document.addEventListener('click', handleClickOutside);
			return () => document.removeEventListener('click', handleClickOutside);
		}
	});
</script>

<header class="top-bar">
	<div class="top-bar-content">
		<!-- Logo -->
		<div class="top-bar-logo">
			<a href={logoHref} class="logo-link">
				<img class="logo-img" src="/ticketing-system-logo.svg" alt={getMessage('app_name')} />
				<!-- <span class="logo-text">{getMessage('app_name')}</span> -->
			</a>
		</div>

		<!-- Right section: Locale switcher & User menu -->
		<div class="top-bar-actions">
			<!-- Locale Switcher -->
			<LanguageSwitcher locale={currentLocale} />

			<!-- User Menu -->
			<div class="user-menu-container">
				<button
					class="topbar-btn user-menu-toggle"
					onclick={toggleUserMenu}
					aria-label={getMessage('user_menu')}
					aria-expanded={showUserMenu}
				>
					<i class="bi bi-person-circle"></i>
					<span class="user-name">{user.name}</span>
					<i class="bi bi-chevron-down"></i>
				</button>

				<div
					class="user-menu-dropdown"
					class:open={showUserMenu}
					aria-hidden={!showUserMenu}
				>
					<div class="user-menu-header">
						<div class="user-info">
							<div class="user-info-name">{user.name}</div>
							<div class="user-info-email">{user.email}</div>
						</div>
						<Badge variant="secondary" size="sm">
						{getMessage(UserRoleKey[userRole as UserRole])}
						</Badge>
					</div>
					<div class="dropdown-divider"></div>
					<button class="dropdown-item" onclick={handleLogout}>
						<i class="bi bi-box-arrow-right"></i>
						{getMessage('logout')}
					</button>
				</div>
			</div>
		</div>
	</div>
</header>

<style>
	.top-bar {
		position: sticky;
		top: 0;
		z-index: 1030;
		background: var(--ironpack-white);
		border-top: 3px solid var(--ironpack-red);
		border-bottom: 1px solid rgba(var(--ironpack-red-rgb), 0.09);
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.05);
	}

	.top-bar-content {
		display: flex;
		align-items: center;
		justify-content: space-between;
		padding: 0.75rem 1.5rem;
		max-width: 100%;
	}

	.top-bar-logo .logo-link {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		text-decoration: none;
		color: var(--ironpack-font-black);
		font-weight: 600;
		font-size: 1.25rem;
	}

	.logo-img {
		height: 2.75em;
		width: auto;
		display: block;
	}

	.top-bar-logo .logo-link:hover {
		color: var(--ironpack-red);
	}

	.top-bar-actions {
		display: flex;
		align-items: center;
		gap: 0.75rem;
	}

	.topbar-btn {
		display: inline-flex;
		align-items: center;
		gap: 0.4rem;
		padding: 0.375rem 0.75rem;
		border-radius: var(--bs-border-radius);
		border: 2px solid rgba(var(--ironpack-red-rgb), 0.09);
		/* border: 1px solid var(--ironpack-red); */
		background: transparent; /* var(--ironpack-red); */
		color: var(--ironpack-font-black);
		cursor: pointer;
		transition: background-color 0.15s ease-in-out, border-color 0.15s ease-in-out;
	}

	.topbar-btn:hover {
		/* background: var(--ironpack-font-black); 
		border-color: var(--ironpack-font-black); */
		color: var(--ironpack-red);
	}

	.topbar-btn .bi {
		color: inherit;
	}

	.user-menu-container {
		position: relative;
	}

	.user-menu-toggle {
		display: flex;
		align-items: center;
		gap: 0.5rem;
		padding: 0.375rem 0.75rem;
	}

	.user-name {
		display: none;
	}

	@media (min-width: 768px) {
		.user-name {
			display: inline;
			max-width: 150px;
			overflow: hidden;
			text-overflow: ellipsis;
			white-space: nowrap;
		}
	}

	.user-menu-dropdown {
		position: absolute;
		top: calc(100% + 0.5rem);
		right: 0;
		min-width: 280px;
		background: var(--bs-white);
		border: 1px solid var(--bs-border-color);
		border-radius: var(--bs-border-radius);
		box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
		z-index: 1050;
		opacity: 0;
		transform: translateY(-4px);
		visibility: hidden;
		pointer-events: none;
		transition: all 0.15s ease-in-out;
	}

	.user-menu-dropdown.open {
		opacity: 1;
		transform: translateY(0);
		visibility: visible;
		pointer-events: auto;
	}

	.user-menu-header {
		padding: 1rem;
		display: flex;
		flex-direction: column;
		gap: 0.5rem;
	}

	.user-info-name {
		font-weight: 600;
		font-size: 1rem;
	}

	.user-info-email {
		font-size: 0.875rem;
		color: var(--bs-secondary-color);
	}

	.dropdown-item {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		width: 100%;
		padding: 0.75rem 1.5rem;
		border: none;
		background: none;
		text-align: left;
		cursor: pointer;
		color: var(--ironpack-font-black);
		transition: all 0.15s ease-in-out;
		white-space: nowrap;
	}

	.dropdown-item:hover {
		background: rgba(var(--ironpack-red-rgb), 0.06);
		color: var(--ironpack-red);
		font-weight: 600;
	}

	.dropdown-item i {
		font-size: 1.25rem;
		min-width: 24px;
		color: var(--ironpack-red);
	}
</style>
