// @vitest-environment node
import { describe, it, expect, beforeAll } from 'vitest';

import { POST } from '../../../src/routes/api/set-locale/+server';

type CookieSetCall = {
	name: string;
	value: string;
	options: any;
};

function ensureBtoaAtobForNode() {
	// SvelteKit runs in environments that provide atob/btoa; Node doesn't by default.
	if (typeof globalThis.btoa === 'undefined') {
		globalThis.btoa = (data: string) => Buffer.from(data, 'utf8').toString('base64');
	}
	if (typeof globalThis.atob === 'undefined') {
		globalThis.atob = (data: string) => Buffer.from(data, 'base64').toString('utf8');
	}
}

function createCookiesMock() {
	const store = new Map<string, string>();
	const setCalls: CookieSetCall[] = [];

	return {
		store,
		setCalls,
		cookies: {
			get: (name: string) => store.get(name),
			set: (name: string, value: string, options: any) => {
				store.set(name, value);
				setCalls.push({ name, value, options });
			},
			delete: (name: string) => {
				store.delete(name);
			}
		}
	};
}

describe('POST /api/set-locale', () => {
	beforeAll(() => {
		ensureBtoaAtobForNode();
	});

	it('sets the locale cookie and returns success=true', async () => {
		const { cookies, setCalls } = createCookiesMock();
		const locale = 'pl-PL';

		const request = new Request('http://localhost/api/set-locale', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ locale })
		});

		const response = await POST({ request, cookies } as any);
		expect(response.status).toBe(200);
		expect(response.headers.get('Content-Type')).toContain('application/json');

		const body = await response.json();
		expect(body).toEqual({ success: true });

		expect(setCalls).toHaveLength(1);
		expect(setCalls[0]!.name).toBe('locale');

		const expectedCookieValue = Buffer.from(JSON.stringify(locale), 'utf8').toString('base64');
		expect(setCalls[0]!.value).toBe(expectedCookieValue);

		// Spot-check important cookie options
		expect(setCalls[0]!.options).toMatchObject({
			path: '/',
			httpOnly: true
		});
	});

	it('does not set a cookie when locale is empty, but still returns success=true', async () => {
		const { cookies, setCalls } = createCookiesMock();

		const request = new Request('http://localhost/api/set-locale', {
			method: 'POST',
			headers: { 'Content-Type': 'application/json' },
			body: JSON.stringify({ locale: '' })
		});

		const response = await POST({ request, cookies } as any);
		expect(response.status).toBe(200);

		const body = await response.json();
		expect(body).toEqual({ success: true });
		expect(setCalls).toHaveLength(0);
	});
});
