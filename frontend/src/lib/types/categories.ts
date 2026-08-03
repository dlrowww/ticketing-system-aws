/** ISO 8601 date strings (e.g., "2025-08-15T10:00:00Z"). */
export type ISODate = string;

export interface PagedResult<T> {
	items: T[];
	total: number;
	page: number;
	size: number;
	totalPages: number;
	hasNext: boolean;
	hasPrevious: boolean;
	sort?: string | null;
}

export interface CategoryDto {
	categoryId: number;
	namePl: string;
	nameEn: string;
	isActive: boolean;
	createdAt: ISODate;
	updatedAt: ISODate | null;
	ticketCount?: number;
}

export interface CreateCategoryRequest {
	namePl: string;
	nameEn: string;
}

export interface UpdateCategoryRequest {
	namePl?: string | null;
	nameEn?: string | null;
	isActive?: boolean | null;
}

export type CategorySortableField = 'categoryId' | 'namePl' | 'nameEn' | 'isActive';

export interface CategoryQuery {
	includeInactive?: boolean;
	sortBy?: CategorySortableField;
	sortDir?: 'asc' | 'desc';
}
