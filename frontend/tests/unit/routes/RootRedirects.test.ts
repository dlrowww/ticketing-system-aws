import { describe, it, expect } from 'vitest';

import { UserRole } from '$lib/types/enums';

describe('Root redirects', () => {
	it('redirects unauthenticated users from / to /login', async () => {
		const { load } = await import('../../../src/routes/+page.server');

		await expect(load({ locals: { user: undefined } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/login'
		});
	});

	it('redirects Admin users from / to /app/dashboard', async () => {
		const { load } = await import('../../../src/routes/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Admin) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/dashboard'
		});
	});

	it('redirects TeamLeader users from / to /app/dashboard', async () => {
		const { load } = await import('../../../src/routes/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.TeamLeader) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/dashboard'
		});
	});

	it('redirects non-Admin/TeamLeader users from / to /app/my-tickets', async () => {
		const { load } = await import('../../../src/routes/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Employee) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/my-tickets'
		});
	});
});

describe('/app redirects', () => {
	it('redirects unauthenticated users from /app to /login', async () => {
		const { load } = await import('../../../src/routes/app/+page.server');

		await expect(load({ locals: { user: undefined } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/login'
		});
	});

	it('redirects Admin users from /app to /app/dashboard', async () => {
		const { load } = await import('../../../src/routes/app/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Admin) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/dashboard'
		});
	});

	it('redirects non-Admin/TeamLeader users from /app to /app/my-tickets', async () => {
		const { load } = await import('../../../src/routes/app/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Support) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/my-tickets'
		});
	});
});

describe('/login redirects', () => {
	it('does not redirect when unauthenticated', async () => {
		const { load } = await import('../../../src/routes/(authentication)/login/+page.server');

		await expect(load({ locals: { user: undefined } } as any)).resolves.toEqual({});
	});

	it('redirects Admin users to /app/dashboard', async () => {
		const { load } = await import('../../../src/routes/(authentication)/login/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Admin) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/dashboard'
		});
	});

	it('redirects non-Admin/TeamLeader users to /app/my-tickets', async () => {
		const { load } = await import('../../../src/routes/(authentication)/login/+page.server');

		await expect(load({ locals: { user: { roleId: String(UserRole.Employee) } } } as any)).rejects.toMatchObject({
			status: 303,
			location: '/app/my-tickets'
		});
	});
});
