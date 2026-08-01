<script lang="ts">
	import { page } from '$app/stores';
	import { getMessage } from '$lib/i18n';
	import { UserRole } from '$lib/types/enums';

	interface User {
		id: string;
		name: string;
		email: string;
		roleId: string;
		categoryId?: string;
	}

	interface NavItem {
		id: string;
		label: string;
		icon: string;
		href?: string;
		roles: UserRole[];
		submenu?: NavItem[];
	}

	let { user }: { user: User } = $props();

	// Mobile starts collapsed, desktop always visible
	let isCollapsed = $state(typeof window !== 'undefined' && window.innerWidth < 768);
	let expandedMenus = $state<Set<string>>(new Set());
	let currentPath = $derived($page.url.pathname);

	// Define navigation items by role
	const navItems: NavItem[] = [
		{
			id: 'dashboard',
			label: 'nav_dashboard',
			icon: 'bi-graph-up',
			href: '/app/dashboard',
			roles: [UserRole.TeamLeader, UserRole.Admin] // Reports access
		},
		{
			id: 'tickets',
			label: 'nav_all_tickets',
			icon: 'bi-list-ul',
			href: '/app/tickets',
			roles: [UserRole.TeamLeader, UserRole.Admin] // TeamLeader, Admin
		},
		{
			id: 'my-tickets',
			label: 'nav_my_requests',
			icon: 'bi-ticket',
			href: '/app/my-tickets',
			roles: [UserRole.Employee, UserRole.Support, UserRole.TeamLeader, UserRole.Admin] // All roles
		},
		{
			id: 'assigned',
			label: 'nav_my_workload',
			icon: 'bi-inbox',
			href: '/app/assigned',
			roles: [UserRole.Support, UserRole.TeamLeader] // Support, TeamLeader
		},
		{
			id: 'unassigned',
			label: 'nav_unassigned_pool',
			icon: 'bi-clipboard',
			href: '/app/unassigned',
			roles: [UserRole.Support] // Support
		},
		{
			id: 'team',
			label: 'nav_team_tickets',
			icon: 'bi-people',
			href: '/app/team',
			roles: [UserRole.TeamLeader] // TeamLeader
		},
		{
			id: 'admin',
			label: 'nav_admin_panel',
			icon: 'bi-gear',
			roles: [UserRole.Admin], // Admin
			submenu: [
				{
					id: 'admin-users',
					label: 'nav_admin_users',
					icon: 'bi-people',
					href: '/app/admin/users',
					roles: [UserRole.Admin]
				},
				{
					id: 'admin-categories',
					label: 'nav_admin_categories',
					icon: 'bi-tags',
					href: '/app/admin/categories',
					roles: [UserRole.Admin]
				}
			]
		}
	];

	// Filter nav items by user role
	const visibleNavItems = $derived.by(() => {
		if (!user || !user.roleId) {
			return [];
		}
		const userRoleNum = Number(user.roleId);
		return navItems.filter((item) => item.roles.includes(userRoleNum as UserRole));
	});

	function toggleCollapse() {
		isCollapsed = !isCollapsed;
	}

	function toggleSubmenu(itemId: string) {
		if (expandedMenus.has(itemId)) {
			expandedMenus.delete(itemId);
		} else {
			expandedMenus.add(itemId);
		}
		expandedMenus = new Set(expandedMenus); // Trigger reactivity
	}

	function isActive(href: string | undefined): boolean {
		if (!href) return false;
		return currentPath === href || currentPath.startsWith(href + '/');
	}

	function hasActiveSubmenu(item: NavItem): boolean {
		if (!item.submenu) return false;
		return item.submenu.some((sub) => isActive(sub.href));
	}

	// Auto-expand admin menu if on admin pages (use untrack to prevent infinite loop)
	$effect(() => {
		if (currentPath.startsWith('/app/admin/')) {
			if (!expandedMenus.has('admin')) {
				expandedMenus.add('admin');
				expandedMenus = new Set(expandedMenus);
			}
		}
	});
</script>

<nav class="nav-rail" class:collapsed={isCollapsed}>
	<!-- Toggle button (mobile) -->
	<button
		class="nav-toggle d-md-none"
		onclick={toggleCollapse}
		aria-label={getMessage('toggle_navigation')}
		aria-expanded={!isCollapsed}
	>
		<i class="bi" class:bi-list={isCollapsed} class:bi-x={!isCollapsed}></i>
	</button>

	<!-- Navigation items -->
	<ul class="nav-items">
		{#each visibleNavItems as item (item.id)}
			<li class="nav-item">
				{#if item.submenu}
					<!-- Item with submenu -->
					<button
						type="button"
						class="nav-link nav-submenu-toggle"
						class:active={hasActiveSubmenu(item)}
						class:expanded={expandedMenus.has(item.id)}
						onclick={() => toggleSubmenu(item.id)}
						aria-label={getMessage(item.label)}
						aria-expanded={expandedMenus.has(item.id)}
					>
						<i class="bi {item.icon}"></i>
						<span class="nav-text">{getMessage(item.label)}</span>
						<i class="bi bi-chevron-down submenu-arrow"></i>
					</button>
					{#if expandedMenus.has(item.id)}
						<ul class="nav-submenu">
							{#each item.submenu as subitem (subitem.id)}
								<li class="nav-submenu-item">
									<a
										href={subitem.href}
										class="nav-link nav-sublink"
										class:active={isActive(subitem.href)}
										aria-label={getMessage(subitem.label)}
									>
										<i class="bi {subitem.icon}"></i>
										<span class="nav-text">{getMessage(subitem.label)}</span>
									</a>
								</li>
							{/each}
						</ul>
					{/if}
				{:else}
					<!-- Regular item -->
					<a
						href={item.href}
						class="nav-link"
						class:active={isActive(item.href)}
						aria-label={getMessage(item.label)}
					>
						<i class="bi {item.icon}"></i>
						<span class="nav-text">{getMessage(item.label)}</span>
					</a>
				{/if}
			</li>
		{/each}
	</ul>
</nav>

<style>
	.nav-rail {
		position: sticky;
		left: 0;
		height: auto;
		width: 240px;
		background: var(--bs-gray-100);
		display: flex;
		flex-direction: column;
		transition: width 0.3s ease, left 0.3s ease;
		z-index: 1020;
		overflow-y: auto;
		flex-shrink: 0; /* Prevent flex shrinking */
	}

	/* Desktop: collapsed means icon-only mode */
	@media (min-width: 768px) {
		.nav-rail {
			width: 240px;
		}

		.nav-rail.collapsed {
			width: 64px; /* Icon-only mode */
		}
	}

	.nav-toggle {
		position: fixed;
		top: 70px;
		left: 0.5rem;
		z-index: 1030;
		width: 40px;
		height: 40px;
		border: 1px solid var(--ironpack-red);
		border-radius: var(--bs-border-radius);
		background: var(--ironpack-red);
		color: var(--ironpack-white);
		display: flex;
		align-items: center;
		justify-content: center;
		cursor: pointer;
		box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
	}

	.nav-toggle:hover {
		background: var(--ironpack-font-black);
		border-color: var(--ironpack-font-black);
	}

	.nav-toggle i {
		font-size: 1.5rem;
	}

	.nav-items {
		list-style: none;
		padding: 1rem 0;
		margin: 0;
	}

	.nav-item {
		margin: 0;
	}

	.nav-link {
		display: flex;
		align-items: center;
		gap: 0.75rem;
		padding: 0.75rem 1.5rem;
		color: var(--ironpack-font-black);
		text-decoration: none;
		transition: all 0.15s ease-in-out;
		white-space: nowrap;
		background: none;
		border: none;
		width: 100%;
		text-align: left;
		cursor: pointer;
	}

	.nav-link:hover {
		background: rgba(var(--ironpack-red-rgb), 0.06);
		color: var(--ironpack-red);
	}

	.nav-link.active {
		background: rgba(var(--ironpack-red-rgb), 0.10);
		color: var(--ironpack-red);
		font-weight: 600;
		border-right: 3px solid var(--ironpack-red);
	}

	.nav-submenu-toggle {
		position: relative;
	}

	.submenu-arrow {
		margin-left: auto;
		font-size: 0.75rem;
		transition: transform 0.2s ease;
	}

	.nav-submenu-toggle.expanded .submenu-arrow {
		transform: rotate(180deg);
	}

	.nav-submenu {
		list-style: none;
		padding: 0;
		margin: 0;
		background: rgba(var(--ironpack-red-rgb), 0.03);
	}

	.nav-submenu-item {
		margin: 0;
	}

	.nav-sublink {
		padding-left: 3rem;
		font-size: 0.9rem;
	}

	.nav-link i {
		font-size: 1.25rem;
		min-width: 24px;
		color: var(--ironpack-red);
	}

	.nav-text {
		overflow: hidden;
		text-overflow: ellipsis;
	}

	.collapsed .nav-text {
		display: none;
	}

	.collapsed .submenu-arrow {
		display: none;
	}

	@media (max-width: 767px) {
		.nav-rail {
			position: fixed;
			top: 61px;
			left: 0;
			width: 240px;
			box-shadow: 2px 0 8px rgba(0, 0, 0, 0.1);
		}

		.nav-rail.collapsed {
			left: -240px;
			width: 240px;
		}
	}
</style>
