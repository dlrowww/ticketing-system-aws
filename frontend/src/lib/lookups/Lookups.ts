// src/lib/lookups/Lookups.ts
import { writable, get } from 'svelte/store';
import { Priority, TicketStatus, UserRole } from '$lib/types/enums';
import type { CategoryDto } from '$lib/stores/categories';

/** One lookup row (id = numeric enum value, name = i18n key). */
export type LookupItem<T extends number> = { id: T; name: string };

/** Payload shape used across the app (FE & BE agree on this). */
export type LookupsPayload = {
  ticketStatus: readonly LookupItem<TicketStatus>[];
  priority: readonly LookupItem<Priority>[];
  category: readonly CategoryDto[]; // Changed: now uses CategoryDto[] from BE
  userRole: readonly LookupItem<UserRole>[];
  version: string; // BE-supplied version/etag/hash
};

/** Optional fallback so the UI can render before hydration (dev-friendly). */
const fallback: LookupsPayload = {
  ticketStatus: [
    { id: TicketStatus.New, name: 'ticket_status_new' },
    { id: TicketStatus.Open, name: 'ticket_status_open' },
    { id: TicketStatus.InProcess, name: 'ticket_status_in_process' },
    { id: TicketStatus.Resolved, name: 'ticket_status_resolved' },
    { id: TicketStatus.Cancelled, name: 'ticket_status_cancelled' },
    { id: TicketStatus.Postponed, name: 'ticket_status_postponed' },
    { id: TicketStatus.Returned, name: 'ticket_status_returned' }
  ] as const,
  priority: [
    { id: Priority.Low, name: 'priority_low' },
    { id: Priority.Medium, name: 'priority_medium' },
    { id: Priority.High, name: 'priority_high' },
    { id: Priority.Critical, name: 'priority_critical' }
  ] as const,
  category: [] as const, // Categories loaded dynamically from /api/categories (see categories store)
  userRole: [
    { id: UserRole.Employee, name: 'role_employee' },
    { id: UserRole.Support, name: 'role_support' },
    { id: UserRole.TeamLeader, name: 'role_team_leader' },
    { id: UserRole.Admin, name: 'role_admin' }
  ] as const,
  version: 'fallback'
};

// --- Svelte store (no data access here) ---
const store = writable<LookupsPayload>(fallback);
export const lookupsStore = { subscribe: store.subscribe };

// Setter/getter so layouts/pages can hydrate this from server data
export function setLookups(payload: LookupsPayload) { store.set(payload); }
export function getLookups(): LookupsPayload { return get(store); }

// Convenient accessors (arrays)
export const lookups = {
  ticketStatus: () => get(store).ticketStatus,
  priority:     () => get(store).priority,
  category:     () => get(store).category,
  userRole:     () => get(store).userRole
};

// Map arrays to dropdown-friendly options { value, labelKey }
export function toOptions<T extends number>(items: readonly LookupItem<T>[] = []) {
  return items.map(i => ({ value: i.id, labelKey: i.name }));
}

// quick finders (id -> i18n key or category name)
export function statusName(id: TicketStatus)  { return get(store).ticketStatus.find(x => x.id === id)?.name; }
export function priorityName(id: Priority)     { return get(store).priority.find(x => x.id === id)?.name; }
// Category name is now fetched from categories store (see getCategoryName in $lib/stores/categories)
export function roleName(id: UserRole)         { return get(store).userRole.find(x => x.id === id)?.name; }