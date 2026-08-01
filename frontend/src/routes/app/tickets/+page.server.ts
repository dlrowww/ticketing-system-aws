import type { PageServerLoad } from './$types';
import { getBackendUrl } from '$lib/server/runtimeEnv';

export const load: PageServerLoad = async ({ fetch, url }) => {
	const backend = getBackendUrl();
	const query = Object.fromEntries(url.searchParams.entries());
	const qs = new URLSearchParams(query).toString();

	try {
		const res = await fetch(`${backend}/api/tickets?${qs}`, { credentials: 'include' });
		if (!res.ok) {
			// return an empty page so the UI still renders
			return {
				initial: {
					items: [],
					total: 0,
					page: Number(query.page ?? 1),
					pageSize: Number(query.pageSize ?? 10)
				},
				query,
				error: `Tickets request failed: ${res.status} ${res.statusText}`
			};
		}
		const data = await res.json();
		return { initial: data, query };
	} catch (e: any) {
		return {
			initial: {
				items: [],
				total: 0,
				page: Number(query.page ?? 1),
				pageSize: Number(query.pageSize ?? 10)
			},
			query,
			error: e?.message ?? 'Network error'
		};
	}
};
