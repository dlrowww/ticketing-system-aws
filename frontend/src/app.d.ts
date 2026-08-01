// See https://svelte.dev/docs/kit/types#app.d.ts
// for information about these interfaces

import type { User } from '$lib/types/user';
import type { LookupsPayload } from '$lib/lookups/Lookups';

declare global {
	namespace App {
		// interface Error {}
		interface Locals {
			user?: User;
			locale?: string;
		}
		interface PageData {
			user?: User;
			locale?: string;
			lookups: LookupsPayload;
		}
		// interface PageState {}
		// interface Platform {}
	}
}

declare module '$env/static/private' {
	export const LOOKUPS_API: string;
	export const BACKEND_URL: string;
	export const JWT_SECRET: string;
}

export {};
