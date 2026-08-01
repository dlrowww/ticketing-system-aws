import { describe, it, expect } from 'vitest';
import { normalizeDateRange } from '$lib/utils/dateRange';

describe('normalizeDateRange', () => {
    it('returns original values when range is valid', () => {
        const result = normalizeDateRange('2025-01-01', '2025-01-10', 'from');
        expect(result).toEqual({ from: '2025-01-01', to: '2025-01-10' });
    });

    it('ignores correction when from is missing', () => {
        const result = normalizeDateRange(undefined, '2025-01-10', 'to');
        expect(result).toEqual({ from: undefined, to: '2025-01-10' });
    });

    it('ignores correction when to is missing', () => {
        const result = normalizeDateRange('2025-01-01', undefined, 'from');
        expect(result).toEqual({ from: '2025-01-01', to: undefined });
    });

    it('moves from forward when to changed to earlier date', () => {
        const result = normalizeDateRange('2025-02-01', '2025-01-01', 'to');
        expect(result).toEqual({ from: '2025-01-01', to: '2025-01-01' });
    });

    it('moves to forward when from changed to later date', () => {
        const result = normalizeDateRange('2025-03-01', '2025-02-01', 'from');
        expect(result).toEqual({ from: '2025-03-01', to: '2025-03-01' });
    });

    it('defaults to adjusting to when last change is unknown', () => {
        const result = normalizeDateRange('2025-04-01', '2025-03-01', null);
        expect(result).toEqual({ from: '2025-04-01', to: '2025-04-01' });
    });
});
