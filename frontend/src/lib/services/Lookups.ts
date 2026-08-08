import type { LookupsPayload } from '$lib/lookups/Lookups';

import { API_BASE } from '$lib/config';
import { getLookups } from '$lib/lookups/Lookups';

const API = `${API_BASE}/lookups`;

const current = getLookups();
const headers: Record<string, string> = {};
if (current?.version) headers['If-None-Match'] = current.version;

export async function fetchLookups(
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<LookupsPayload> {
	const res = await fetchFn(API, {
		credentials: 'include',
		signal,
		headers
	});

	// On some runtimes 304 may be returned if the platform/browser handles ETag;
	// fall back to current store (already-hydrated) to avoid breaking UI.
	if (res.status === 304) {
		return getLookups();
	}

	if (!res.ok) {
		const text = await res.text().catch(() => '');
		throw new Error(`Lookups failed: ${res.status} ${res.statusText} ${text ? '- ' + text : ''}`);
	}

	// Expected BE payload shape:
	// {
	//   ticketStatus: [{ id: number, name: string }, ...],
	//   priority:     [{ id: number, name: string }, ...],
	//   category:     [{ id: number, name: string }, ...],
	//   userRole:     [{ id: number, name: string }, ...],
	//   version: string
	// }
	const data = (await res.json()) as LookupsPayload;
	if (!data || !data.category || !data.priority || !data.userRole) {
		throw new Error('Lookups payload missing required fields.');
	}
	return data as LookupsPayload;
}
