import { getBackendUrl } from '$lib/server/runtimeEnv';
import type {
	PagedResult,
	TicketListItem,
	TicketQuery,
	TicketSortableField
} from '$lib/types/tickets';

export type FixedTicketFilters = {
	createdByUserId?: number;
	/** Set to null to filter unassigned tickets. */
	assignedToUserId?: number | null;
	category?: number;
};

function toNumberOrUndefined(v: string | null): number | undefined {
	if (v === null || v.trim() === '') return undefined;
	const n = Number(v);
	return Number.isFinite(n) ? n : undefined;
}

const ticketSortableFields: readonly TicketSortableField[] = [
	'ticketId',
	'title',
	'category',
	'priority',
	'status',
	'createdAt',
	'updatedAt',
	'createdByName',
	'assignedToName'
];

function toTicketSortableField(v: string | null): TicketSortableField | undefined {
	if (!v) return undefined;
	return (ticketSortableFields as readonly string[]).includes(v)
		? (v as TicketSortableField)
		: undefined;
}

export function buildTicketsSearchParams(url: URL, fixed?: FixedTicketFilters): URLSearchParams {
	const params = new URLSearchParams(url.searchParams);

	if (typeof fixed?.createdByUserId === 'number') {
		params.set('createdByUserId', String(fixed.createdByUserId));
	}

	if (fixed && 'assignedToUserId' in fixed) {
		if (fixed.assignedToUserId === null) {
			// Signal "unassigned" by sending the param with empty value.
			params.set('assignedToUserId', '');
		} else if (typeof fixed.assignedToUserId === 'number') {
			params.set('assignedToUserId', String(fixed.assignedToUserId));
		}
	}

	if (typeof fixed?.category === 'number') {
		params.set('category', String(fixed.category));
	}

	return params;
}

export function emptyTicketsPage(params: URLSearchParams): PagedResult<TicketListItem> {
	const page = toNumberOrUndefined(params.get('page')) ?? 1;
	const size = toNumberOrUndefined(params.get('pageSize')) ?? 10;

	return {
		items: [],
		total: 0,
		page,
		size,
		totalPages: 1,
		hasNext: false,
		hasPrevious: false,
		sort: null
	};
}

export async function loadTicketsListPage(args: {
	fetch: typeof fetch;
	url: URL;
	fixed?: FixedTicketFilters;
}): Promise<{ initial: PagedResult<TicketListItem>; query: TicketQuery; error?: string }> {
	const backend = getBackendUrl();
	const params = buildTicketsSearchParams(args.url, args.fixed);
	const qs = params.toString();

	// Provide a strongly-typed query object for the UI.
	const status = toNumberOrUndefined(params.get('status'));
	const category = toNumberOrUndefined(params.get('category'));
	const priority = toNumberOrUndefined(params.get('priority'));

	const query: TicketQuery = {
		page: toNumberOrUndefined(params.get('page')),
		pageSize: toNumberOrUndefined(params.get('pageSize')),
		sortBy: toTicketSortableField(params.get('sortBy')),
		sortDir: (params.get('sortDir') as 'asc' | 'desc' | null) ?? undefined,
		search: params.get('search') ?? undefined,
		status: (status ?? undefined) as any,
		category: (category ?? undefined) as any,
		priority: (priority ?? undefined) as any,
		dateFrom: params.get('dateFrom') ?? undefined,
		dateTo: params.get('dateTo') ?? undefined,
		createdByUserId: args.fixed?.createdByUserId,
		assignedToUserId:
			args.fixed && 'assignedToUserId' in args.fixed ? args.fixed.assignedToUserId : undefined
	};

	try {
		const res = await args.fetch(`${backend}/api/tickets?${qs}`, { credentials: 'include' });
		if (!res.ok) {
			return {
				initial: emptyTicketsPage(params),
				query,
				error: `Tickets request failed: ${res.status} ${res.statusText}`
			};
		}
		const data = (await res.json()) as PagedResult<TicketListItem>;
		return { initial: data, query };
	} catch (e: any) {
		return {
			initial: emptyTicketsPage(params),
			query,
			error: e?.message ?? 'Network error'
		};
	}
}
