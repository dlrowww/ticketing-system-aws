import { get } from 'svelte/store';
import { locale as i18nLocale } from 'svelte-i18n';

const DEFAULT_LOCALE = 'en-US';

type SupportedLocale = 'en-US' | 'pl-PL';

const LOCALE_MAP: Record<string, SupportedLocale> = {
    'en': 'en-US',
    'en-us': 'en-US',
    'en-gb': 'en-US',
    'pl': 'pl-PL',
    'pl-pl': 'pl-PL'
};

const TABLE_FORMATTERS: Record<SupportedLocale, Intl.DateTimeFormat> = {
    'en-US': new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    }),
    'pl-PL': new Intl.DateTimeFormat('pl-PL', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric',
        hour: '2-digit',
        minute: '2-digit',
        hour12: false
    })
};

const PICKER_FORMATTERS: Record<SupportedLocale, Intl.DateTimeFormat> = {
    'en-US': new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    }),
    'pl-PL': new Intl.DateTimeFormat('pl-PL', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    })
};

const MONTH_YEAR_FORMATTERS: Record<SupportedLocale, Intl.DateTimeFormat> = {
    'en-US': new Intl.DateTimeFormat('en-US', {
        month: 'long',
        year: 'numeric'
    }),
    'pl-PL': new Intl.DateTimeFormat('pl-PL', {
        month: 'long',
        year: 'numeric'
    })
};

const TREND_AXIS_FORMATTERS: Record<SupportedLocale, Intl.DateTimeFormat> = {
    'en-US': new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric'
    }),
    'pl-PL': new Intl.DateTimeFormat('pl-PL', {
        day: '2-digit',
        month: '2-digit'
    })
};

const SUBTITLE_DATE_FORMATTERS: Record<SupportedLocale, Intl.DateTimeFormat> = {
    'en-US': new Intl.DateTimeFormat('en-US', {
        month: 'short',
        day: 'numeric',
        year: 'numeric'
    }),
    'pl-PL': new Intl.DateTimeFormat('pl-PL', {
        day: '2-digit',
        month: '2-digit',
        year: 'numeric'
    })
};

function resolveLocale(raw?: string | null): SupportedLocale {
    if (!raw) return DEFAULT_LOCALE;
    const key = raw.toLowerCase().replace(/_/g, '-');
    return LOCALE_MAP[key] ?? (key.startsWith('pl') ? 'pl-PL' : 'en-US');
}

function ensureDate(value: string | Date): Date | null {
    const date = typeof value === 'string' ? new Date(value) : value;
    if (!(date instanceof Date) || Number.isNaN(date.getTime())) return null;
    return date;
}

function currentLocale(): SupportedLocale {
    const raw = get(i18nLocale);
    return resolveLocale(raw);
}

export function formatDateTimeForTable(value: string | Date | null | undefined): string {
    if (!value) return '-';
    const date = ensureDate(value);
    if (!date) return '-';
    const formatter = TABLE_FORMATTERS[currentLocale()];
    return formatter.format(date);
}

export function formatDateForPicker(value: string | Date | null | undefined): string {
    if (!value) return '';
    const date = ensureDate(value);
    if (!date) return '';
    const formatter = PICKER_FORMATTERS[currentLocale()];
    return formatter.format(date);
}

export function formatDateTimeForDetails(value: string | Date | null | undefined): string {
    if (!value) return '-';
    const date = ensureDate(value);
    if (!date) return '-';
    const formatter = TABLE_FORMATTERS[currentLocale()];
    return formatter.format(date);
}

export function formatMonthYearHeading(value: Date): string {
    const formatter = MONTH_YEAR_FORMATTERS[currentLocale()];
    return formatter.format(value);
}

export function formatDateForTrendAxis(value: string | Date | null | undefined): string {
    if (!value) return '';
    const date = ensureDate(value);
    if (!date) return '';
    const formatter = TREND_AXIS_FORMATTERS[currentLocale()];
    return formatter.format(date);
}

export function formatDateForFiltersSubtitle(value: string | Date | null | undefined): string {
    if (!value) return '';
    const date = ensureDate(value);
    if (!date) return '';
    const formatter = SUBTITLE_DATE_FORMATTERS[currentLocale()];
    return formatter.format(date);
}
