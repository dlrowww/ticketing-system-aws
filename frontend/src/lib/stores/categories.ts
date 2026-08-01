import { writable, derived, get } from 'svelte/store';
import { getMessage } from '$lib/i18n';
import { locale as _locale } from 'svelte-i18n';

/**
 * Category DTO from backend
 */
export interface CategoryDto {
	categoryId: number;
	namePl: string;
	nameEn: string;
	isActive: boolean;
	createdAt: string;
	updatedAt: string | null;
}

// Category store (populated from SSR data)
export const categories = writable<CategoryDto[]>([]);

// Derived store: categoryId → CategoryDto map
export const categoryMap = derived(categories, ($categories) => {
	const map = new Map<number, CategoryDto>();
	$categories.forEach((cat) => map.set(cat.categoryId, cat));
	return map;
});

/**
 * Get category name by ID (respects current locale)
 * Admin controls category names (namePl/nameEn), not i18n translations
 * @param categoryId - The category ID
 * @returns Category name in current locale, or i18n 'category_unknown' if not found
 */
export function getCategoryName(categoryId: number): string {
	const currentLocale = get(_locale); // Get current locale (e.g., 'pl-PL', 'en-US')
	const map = get(categoryMap);
	const category = map.get(categoryId);

	if (!category) return getMessage('category_unknown');

	// Return DB-driven name based on locale (Admin controls these names)
	// Extract language code (pl-PL → pl, en-US → en)
	const lang = currentLocale?.split('-')[0] || 'en';
	return lang === 'pl' ? category.namePl : category.nameEn;
}

/**
 * Reactive version: Get category name (updates when locale or categories change)
 * @param categoryId - The category ID
 * @returns Derived store with category name
 */
export function getCategoryNameReactive(categoryId: number) {
	return derived([categoryMap, _locale], ([$map, $locale]) => {
		const category = $map.get(categoryId);
		if (!category) return getMessage('category_unknown');

		// Extract language code (pl-PL → pl, en-US → en)
		const lang = $locale?.split('-')[0] || 'en';
		return lang === 'pl' ? category.namePl : category.nameEn;
	});
}
