import { describe, it, expect, vi, beforeEach, afterEach } from 'vitest';
import { get } from 'svelte/store';
import { toastStore } from '$lib/stores/toast';
import type { ToastType } from '$lib/stores/toast';

describe('Toast Store', () => {
    beforeEach(() => {
        // Clear all toasts before each test
        toastStore.clear();
        vi.useFakeTimers();
    });

    afterEach(() => {
        vi.restoreAllMocks();
    });

    it('should start with empty toasts array', () => {
        const state = get(toastStore);
        expect(state.toasts).toEqual([]);
    });

    it('should add a toast notification', () => {
        toastStore.add('success', 'Test message', 0);
        const state = get(toastStore);
        
        expect(state.toasts).toHaveLength(1);
        expect(state.toasts[0]).toMatchObject({
            type: 'success',
            message: 'Test message',
            duration: 0
        });
        expect(state.toasts[0].id).toMatch(/^toast-/);
    });

    it('should add multiple toasts', () => {
        toastStore.add('success', 'Message 1', 0);
        toastStore.add('error', 'Message 2', 0);
        toastStore.add('info', 'Message 3', 0);
        
        const state = get(toastStore);
        expect(state.toasts).toHaveLength(3);
        expect(state.toasts[0].message).toBe('Message 1');
        expect(state.toasts[1].message).toBe('Message 2');
        expect(state.toasts[2].message).toBe('Message 3');
    });

    it('should remove a toast by ID', () => {
        const id1 = toastStore.add('success', 'Message 1', 0);
        const id2 = toastStore.add('error', 'Message 2', 0);
        
        toastStore.remove(id1);
        
        const state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
        expect(state.toasts[0].id).toBe(id2);
    });

    it('should clear all toasts', () => {
        toastStore.add('success', 'Message 1', 0);
        toastStore.add('error', 'Message 2', 0);
        toastStore.add('info', 'Message 3', 0);
        
        toastStore.clear();
        
        const state = get(toastStore);
        expect(state.toasts).toEqual([]);
    });

    it('should auto-dismiss toast after specified duration', () => {
        toastStore.add('success', 'Auto-dismiss me', 3000);
        
        let state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
        
        // Fast-forward time by 3 seconds
        vi.advanceTimersByTime(3000);
        
        state = get(toastStore);
        expect(state.toasts).toHaveLength(0);
    });

    it('should not auto-dismiss when duration is 0', () => {
        toastStore.add('success', 'Persistent toast', 0);
        
        let state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
        
        // Fast-forward time significantly
        vi.advanceTimersByTime(10000);
        
        state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
    });

    it('should use default duration of 3000ms when not specified', () => {
        toastStore.add('success', 'Default duration');
        
        let state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
        expect(state.toasts[0].duration).toBe(3000);
        
        vi.advanceTimersByTime(3000);
        
        state = get(toastStore);
        expect(state.toasts).toHaveLength(0);
    });

    describe('Convenience methods', () => {
        it('should add success toast using convenience method', () => {
            toastStore.success('Success message', 0);
            
            const state = get(toastStore);
            expect(state.toasts).toHaveLength(1);
            expect(state.toasts[0].type).toBe('success');
            expect(state.toasts[0].message).toBe('Success message');
        });

        it('should add error toast using convenience method', () => {
            toastStore.error('Error message', 0);
            
            const state = get(toastStore);
            expect(state.toasts).toHaveLength(1);
            expect(state.toasts[0].type).toBe('error');
            expect(state.toasts[0].message).toBe('Error message');
        });

        it('should add warning toast using convenience method', () => {
            toastStore.warning('Warning message', 0);
            
            const state = get(toastStore);
            expect(state.toasts).toHaveLength(1);
            expect(state.toasts[0].type).toBe('warning');
            expect(state.toasts[0].message).toBe('Warning message');
        });

        it('should add info toast using convenience method', () => {
            toastStore.info('Info message', 0);
            
            const state = get(toastStore);
            expect(state.toasts).toHaveLength(1);
            expect(state.toasts[0].type).toBe('info');
            expect(state.toasts[0].message).toBe('Info message');
        });
    });

    it('should generate unique IDs for each toast', () => {
        const id1 = toastStore.add('success', 'Message 1', 0);
        const id2 = toastStore.add('success', 'Message 2', 0);
        const id3 = toastStore.add('success', 'Message 3', 0);
        
        expect(id1).not.toBe(id2);
        expect(id2).not.toBe(id3);
        expect(id1).not.toBe(id3);
    });

    it('should handle multiple auto-dismiss toasts with different durations', () => {
        toastStore.add('success', 'Quick toast', 1000);
        toastStore.add('info', 'Medium toast', 2000);
        toastStore.add('warning', 'Slow toast', 3000);
        
        let state = get(toastStore);
        expect(state.toasts).toHaveLength(3);
        
        // After 1 second, first toast should be gone
        vi.advanceTimersByTime(1000);
        state = get(toastStore);
        expect(state.toasts).toHaveLength(2);
        
        // After 2 seconds total, second toast should be gone
        vi.advanceTimersByTime(1000);
        state = get(toastStore);
        expect(state.toasts).toHaveLength(1);
        
        // After 3 seconds total, all toasts should be gone
        vi.advanceTimersByTime(1000);
        state = get(toastStore);
        expect(state.toasts).toHaveLength(0);
    });
});
