// src/lib/types/table.ts
/** Generic data table types for reusable table components */

import type { Component } from 'svelte';

/** Base sortable field type - can be extended for specific tables */
export type SortableField = string;

/** Sort direction */
export type SortDirection = 'asc' | 'desc';

/** Sorting configuration */
export interface Sorting<F extends string = SortableField> {
	sortBy?: F;
	sortDir?: SortDirection;
}

/** Pagination configuration */
export interface Paging {
	/** 1-based page index */
	page?: number;
	pageSize?: number;
}

/** Column formatter function - returns formatted string or component */
export type ColumnFormatter<T = any> = (value: any, row: T) => string | any;

/** Table column definition */
export interface TableColumn<T = any> {
	/** Column key (must match a property in the data type) */
	key: string;
	/** i18n label key for column header */
	label: string;
	/** Column width (e.g., 'auto', '40%', '200px') */
	width?: string;
	/** Whether column is sortable */
	sortable?: boolean;
	/** Optional formatter for cell value */
	formatter?: ColumnFormatter<T>;
	/** Custom class for column cells */
	cellClass?: string;
	/** Custom class for header cell */
	headerClass?: string;
}

/** Table action definition */
export interface TableAction {
	/** Action identifier */
	id: string;
	/** i18n label key */
	label: string;
	/** Bootstrap icon class (e.g., 'bi-eye', 'bi-pencil') */
	icon?: string;
	/** Whether action requires row selection (for bulk actions) */
	requiresSelection?: boolean;
	/** Optional function to determine if action should be shown for a row */
	conditional?: (row: any) => boolean;
}

/** Generic data table configuration */
export interface DataTableConfig<T = any> {
	/** Unique key field for row identification */
	keyField: keyof T;
	/** Column definitions */
	columns: TableColumn<T>[];
	/** Row actions (displayed in actions column) */
	actions?: TableAction[];
	/** Bulk actions (displayed when rows are selected) */
	bulkActions?: TableAction[];
	/** Enable row selection checkboxes */
	enableSelection?: boolean;
	/** Enable column sorting */
	enableSorting?: boolean;
}

/** Pagination info */
export interface PaginationInfo {
	currentPage: number;
	totalPages: number;
	pageSize: number;
	totalItems: number;
	hasNext: boolean;
	hasPrevious: boolean;
}

/** Row action event payload */
export interface RowActionEvent {
	action: string;
	id: number | string;
}

/** Bulk action event payload */
export interface BulkActionEvent {
	action: string;
	ids: (number | string)[];
}
