import { describe, it, expect, beforeEach, vi } from 'vitest';
import { locale } from 'svelte-i18n';
import { formatDateForFiltersSubtitle } from '$lib/utils/dateTime';

vi.mock('svelte-i18n', async () => {
    const actual = await vi.importActual('svelte-i18n');
    return {
        ...actual,
        locale: {
            subscribe: vi.fn((run: (val: string | null) => void) => {
                run('en-US');
                return () => undefined;
            })
        }
    };
});

function mockLocale(value: string) {
    vi.mocked(locale.subscribe).mockImplementation((run: (val: string | null) => void) => {
        run(value);
        return () => undefined;
    });
}

describe('formatDateForFiltersSubtitle', () => {
    beforeEach(() => {
        mockLocale('en-US');
    });

    it('returns empty string for falsy values', () => {
        expect(formatDateForFiltersSubtitle(undefined)).toBe('');
        expect(formatDateForFiltersSubtitle(null as any)).toBe('');
    });

    it('formats dates for en-US locale', () => {
        expect(formatDateForFiltersSubtitle('2026-01-05')).toBe('Jan 5, 2026');
    });

    it('formats dates for pl-PL locale', () => {
        mockLocale('pl-PL');
        expect(formatDateForFiltersSubtitle('2026-01-05')).toBe('05.01.2026');
    });

    it('falls back gracefully on invalid dates', () => {
        expect(formatDateForFiltersSubtitle('invalid-date')).toBe('');
    });
});
