import type { UserRole } from '$lib/types/enums';

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

export interface UserListItemDto {
	userId: number;
	name: string;
	email: string;
	role: UserRole;
	categoryId?: number | null;
	isActive: boolean;
}

export interface UserDetailsDto extends UserListItemDto {}

export interface CreateUserRequest {
	name: string;
	email: string;
	password: string;
	role: UserRole;
	categoryId?: number | null;
}

export interface UpdateUserRequest {
	name?: string | null;
	email?: string | null;
	password?: string | null;
	role?: UserRole | null;
	categoryId?: number | null;
	isActive?: boolean | null;
}

export interface AssignableUserDto {
	userId: number;
	name: string;
	email: string;
	roleName: string;
	categoryId: number | null;
	categoryNamePl: string | null;
	categoryNameEn: string | null;
}

export type UserSortableField = 'userId' | 'name' | 'email' | 'role' | 'categoryId' | 'isActive';

export interface UserQuery {
	page?: number;
	pageSize?: number;
	sortBy?: UserSortableField;
	sortDir?: 'asc' | 'desc';
	search?: string;
	role?: UserRole | 'All';
	category?: number | 'All';
	isActive?: 'true' | 'false' | 'All';
}
