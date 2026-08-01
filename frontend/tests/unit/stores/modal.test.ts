import { describe, it, expect, vi, beforeEach } from 'vitest';
import { get } from 'svelte/store';
import { modalStore } from '$lib/stores/modal';
import type { ModalOptions } from '$lib/stores/modal';

// Mock component for testing
const MockComponent = {} as any;

describe('Modal Store', () => {
    beforeEach(() => {
        // Close modal before each test
        modalStore.close();
    });

    it('should start with modal closed', () => {
        const state = get(modalStore);
        expect(state.isOpen).toBe(false);
        expect(state.options).toBeNull();
    });

    it('should open a modal with component and default options', () => {
        const options: ModalOptions = {
            component: MockComponent
        };
        
        modalStore.open(options);
        
        const state = get(modalStore);
        expect(state.isOpen).toBe(true);
        expect(state.options).toMatchObject({
            component: MockComponent,
            size: 'md',
            closeOnBackdrop: false,
            closeOnEscape: true
        });
    });

    it('should open a modal with custom options', () => {
        const options: ModalOptions = {
            component: MockComponent,
            props: { title: 'Test Modal', id: 123 },
            size: 'lg',
            closeOnBackdrop: false,
            closeOnEscape: false
        };
        
        modalStore.open(options);
        
        const state = get(modalStore);
        expect(state.isOpen).toBe(true);
        expect(state.options).toMatchObject({
            component: MockComponent,
            props: { title: 'Test Modal', id: 123 },
            size: 'lg',
            closeOnBackdrop: false,
            closeOnEscape: false
        });
    });

    it('should close an open modal', () => {
        modalStore.open({ component: MockComponent });
        
        let state = get(modalStore);
        expect(state.isOpen).toBe(true);
        
        modalStore.close();
        
        state = get(modalStore);
        expect(state.isOpen).toBe(false);
        expect(state.options).toBeNull();
    });

    it('should call onClose callback when modal closes', () => {
        const onClose = vi.fn();
        
        modalStore.open({
            component: MockComponent,
            onClose
        });
        
        modalStore.close();
        
        expect(onClose).toHaveBeenCalledTimes(1);
    });

    it('should replace existing modal when opening a new one', () => {
        const firstComponent = {} as any;
        const secondComponent = {} as any;
        
        modalStore.open({ component: firstComponent });
        
        let state = get(modalStore);
        expect(state.options?.component).toBe(firstComponent);
        
        modalStore.open({ component: secondComponent });
        
        state = get(modalStore);
        expect(state.options?.component).toBe(secondComponent);
        expect(state.isOpen).toBe(true);
    });

    it('should return correct isOpen status', () => {
        expect(modalStore.isOpen()).toBe(false);
        
        modalStore.open({ component: MockComponent });
        expect(modalStore.isOpen()).toBe(true);
        
        modalStore.close();
        expect(modalStore.isOpen()).toBe(false);
    });

    it('should handle closing when no modal is open', () => {
        // Should not throw error
        expect(() => modalStore.close()).not.toThrow();
        
        const state = get(modalStore);
        expect(state.isOpen).toBe(false);
        expect(state.options).toBeNull();
    });

    it('should support all modal sizes', () => {
        const sizes: Array<'sm' | 'md' | 'lg' | 'xl'> = ['sm', 'md', 'lg', 'xl'];
        
        sizes.forEach(size => {
            modalStore.open({
                component: MockComponent,
                size
            });
            
            const state = get(modalStore);
            expect(state.options?.size).toBe(size);
            
            modalStore.close();
        });
    });

    it('should preserve props when opening modal', () => {
        const props = {
            id: 42,
            title: 'Test',
            data: { nested: 'value' },
            callback: () => 'test'
        };
        
        modalStore.open({
            component: MockComponent,
            props
        });
        
        const state = get(modalStore);
        expect(state.options?.props).toEqual(props);
    });

    it('should not call onClose when modal is already closed', () => {
        const onClose = vi.fn();
        
        modalStore.open({
            component: MockComponent,
            onClose
        });
        
        modalStore.close();
        expect(onClose).toHaveBeenCalledTimes(1);
        
        // Close again - should not call onClose again
        modalStore.close();
        expect(onClose).toHaveBeenCalledTimes(1);
    });
});
