import { describe, it, expect } from 'vitest';
import { toOptions, statusName, priorityName, roleName, setLookups, getLookups } from '$lib/lookups/Lookups';
import type { TicketStatus, Priority, UserRole } from '$lib/types/enums';
import type { CategoryDto } from '$lib/stores/categories';

describe('Lookups', () => {
  describe('toOptions', () => {
    it('converts enum items to select options', () => {
      const mockItems = [
        { id: 1 as any, name: 'option_one' },
        { id: 2 as any, name: 'option_two' },
        { id: 3 as any, name: 'option_three' }
      ];

      const options = toOptions(mockItems);

      expect(options).toHaveLength(3);
      expect(options[0]).toEqual({ value: 1, labelKey: 'option_one' });
      expect(options[1]).toEqual({ value: 2, labelKey: 'option_two' });
      expect(options[2]).toEqual({ value: 3, labelKey: 'option_three' });
    });

    it('returns empty array when given empty array', () => {
      const options = toOptions([]);
      expect(options).toEqual([]);
    });

    it('returns empty array when given undefined', () => {
      const options = toOptions(undefined);
      expect(options).toEqual([]);
    });
  });

  describe('statusName', () => {
    it('returns correct i18n key for status', () => {
      const name = statusName(1 as TicketStatus);
      expect(name).toBe('ticket_status_new');
    });

    it('returns undefined for non-existent status', () => {
      const name = statusName(999 as TicketStatus);
      expect(name).toBeUndefined();
    });
  });

  describe('priorityName', () => {
    it('returns correct i18n key for priority', () => {
      const name = priorityName(1 as Priority);
      expect(name).toBe('priority_low');
    });

    it('returns undefined for non-existent priority', () => {
      const name = priorityName(999 as Priority);
      expect(name).toBeUndefined();
    });
  });

  describe('roleName', () => {
    it('returns correct i18n key for role', () => {
      const name = roleName(1 as UserRole);
      expect(name).toBe('role_employee');
    });

    it('returns undefined for non-existent role', () => {
      const name = roleName(999 as UserRole);
      expect(name).toBeUndefined();
    });
  });

  describe('setLookups and getLookups', () => {
    it('allows setting and getting lookups', () => {
      const customLookups = {
        ticketStatus: [{ id: 1 as TicketStatus, name: 'custom_status' }],
        priority: [{ id: 1 as Priority, name: 'custom_priority' }],
        category: [{ categoryId: 1, nameEn: 'IT', namePl: 'IT', isActive: true } as CategoryDto],
        userRole: [{ id: 1 as UserRole, name: 'custom_role' }],
        version: 'custom'
      };

      setLookups(customLookups);
      const retrieved = getLookups();

      expect(retrieved.version).toBe('custom');
      expect(retrieved.ticketStatus[0].name).toBe('custom_status');
    });
  });
});
