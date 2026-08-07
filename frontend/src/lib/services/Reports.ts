import { API_BASE } from '$lib/config';
import type {
	DashboardStats,
	TicketCountByCategory,
	TicketCountByStatus,
	TicketCountByPriority,
	TicketTrendPoint
} from '$lib/types/reports';

const API = `${API_BASE}/reports`;

function dateParams(from?: string | null, to?: string | null) {
	const params = new URLSearchParams();
	// Only add params if they have actual values (not null, undefined, or empty string)
	if (from && from.trim()) params.set('from', from.trim());
	if (to && to.trim()) params.set('to', to.trim());
	return params;
}

export async function fetchDashboardStats(
	args: { from?: string | null; to?: string | null },
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<DashboardStats> {
	const params = dateParams(args.from, args.to);
	const res = await fetchFn(`${API}/dashboard?${params.toString()}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok) throw new Error(`Failed to load dashboard stats: ${res.status} ${res.statusText}`);
	return (await res.json()) as DashboardStats;
}

export async function fetchTicketsByCategory(
	args: { from?: string | null; to?: string | null },
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketCountByCategory[]> {
	const params = dateParams(args.from, args.to);
	const res = await fetchFn(`${API}/tickets-by-category?${params.toString()}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok)
		throw new Error(`Failed to load tickets by category: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketCountByCategory[];
}

export async function fetchTicketsByStatus(
	args: { from?: string | null; to?: string | null },
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketCountByStatus[]> {
	const params = dateParams(args.from, args.to);
	const res = await fetchFn(`${API}/tickets-by-status?${params.toString()}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok) throw new Error(`Failed to load tickets by status: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketCountByStatus[];
}

export async function fetchTicketsByPriority(
	args: { from?: string | null; to?: string | null },
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketCountByPriority[]> {
	const params = dateParams(args.from, args.to);
	const res = await fetchFn(`${API}/tickets-by-priority?${params.toString()}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok)
		throw new Error(`Failed to load tickets by priority: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketCountByPriority[];
}

export async function fetchTicketTrend(
	args: { from?: string | null; to?: string | null; days?: number },
	fetchFn: typeof fetch = fetch,
	signal?: AbortSignal
): Promise<TicketTrendPoint[]> {
	const params = dateParams(args.from, args.to);
	if (args.days) params.set('days', String(args.days));
	const res = await fetchFn(`${API}/ticket-trend?${params.toString()}`, {
		credentials: 'include',
		signal
	});
	if (!res.ok) throw new Error(`Failed to load ticket trend: ${res.status} ${res.statusText}`);
	return (await res.json()) as TicketTrendPoint[];
}
