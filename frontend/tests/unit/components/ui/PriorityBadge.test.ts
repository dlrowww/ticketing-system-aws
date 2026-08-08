import { describe, it, expect } from 'vitest';
import { render } from '@testing-library/svelte';
import PriorityBadge from '$lib/components/tickets/PriorityBadge.svelte';
import { Priority } from '$lib/types/enums';

describe('PriorityBadge Component', () => {
	it('should render Low priority with green badge', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.Low }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toBeTruthy();
		expect(badge?.classList.contains('bg-priority-low')).toBe(true);
		expect(badge?.querySelector('.bi-arrow-down')).toBeTruthy();
	});

	it('should render Medium priority with yellow badge', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.Medium }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toBeTruthy();
		expect(badge?.classList.contains('bg-priority-medium')).toBe(true);
		expect(badge?.querySelector('.bi-dash')).toBeTruthy();
	});

	it('should render High priority with orange badge', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.High }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toBeTruthy();
		expect(badge?.classList.contains('bg-priority-high')).toBe(true);
		expect(badge?.querySelector('.bi-arrow-up')).toBeTruthy();
	});

	it('should render Critical priority with red badge', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.Critical }
		});
		const badge = container.querySelector('.badge');

		expect(badge).toBeTruthy();
		expect(badge?.classList.contains('bg-priority-critical')).toBe(true);
		expect(badge?.querySelector('.bi-exclamation-triangle-fill')).toBeTruthy();
	});

	it('should display localized priority text for Low', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.Low }
		});
		const badge = container.querySelector('.badge');

		// Note: Actual text depends on i18n locale, but enum should be present
		expect(badge?.textContent).toBeTruthy();
	});

	it('should handle unknown priority gracefully', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: 99 as Priority } // Invalid priority
		});
		const badge = container.querySelector('.badge');

		// Should still render a badge (fallback to secondary)
		expect(badge).toBeTruthy();
	});

	it('should have correct icon spacing', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.High }
		});
		const icon = container.querySelector('i.bi');
		expect(icon?.getAttribute('style') ?? '').toContain('margin-right');
	});

	it('should use Bootstrap badge classes', () => {
		const { container } = render(PriorityBadge, {
			props: { priority: Priority.Medium }
		});
		const badge = container.querySelector('.badge');

		expect(badge?.classList.contains('badge')).toBe(true);
	});
});
