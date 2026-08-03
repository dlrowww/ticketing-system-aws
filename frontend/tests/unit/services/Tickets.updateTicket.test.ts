import { describe, it, expect, vi, beforeEach } from 'vitest';
import { updateTicket } from '$lib/services/Tickets';

// Mock fetch globally
global.fetch = vi.fn();

describe('Tickets Service - updateTicket', () => {
	beforeEach(() => {
		vi.resetAllMocks();
	});

	it('should successfully update ticket with partial data', async () => {
		const mockResponse = {
			ticketId: 123,
			title: 'Updated Title',
			description: 'Updated Description',
			category: 1,
			categoryName: 'IT',
			priority: 3,
			priorityName: 'High',
			status: 2,
			statusName: 'In Progress',
			createdBy: 100,
			createdByName: 'Creator',
			createdAt: '2025-01-01T10:00:00Z',
			updatedAt: '2025-01-04T14:30:00Z',
			assignedToUserId: 200,
			assignedToName: 'Assignee',
			capabilities: {
				canEditTitle: true,
				canEditDescription: true,
				canEditCategory: false,
				canEditPriority: true,
				canEditStatus: true,
				canEditAssignment: true
			}
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		const request = {
			title: 'Updated Title',
			priority: 3
		};

		const result = await updateTicket(123, request);

		expect(fetch).toHaveBeenCalledWith(
			'/api/tickets/123',
			expect.objectContaining({
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify(request),
				credentials: 'include'
			})
		);

		expect(result).toEqual(mockResponse);
	});

	it('should successfully update ticket with all fields', async () => {
		const mockResponse = {
			ticketId: 123,
			title: 'Completely Updated',
			description: 'All fields changed',
			category: 2,
			categoryName: 'Logistics',
			priority: 4,
			priorityName: 'Critical',
			status: 3,
			statusName: 'Resolved',
			createdBy: 100,
			createdByName: 'Creator',
			createdAt: '2025-01-01T10:00:00Z',
			updatedAt: '2025-01-04T15:00:00Z',
			assignedToUserId: 300,
			assignedToName: 'New Assignee',
			capabilities: {
				canEditTitle: false,
				canEditDescription: false,
				canEditCategory: false,
				canEditPriority: false,
				canEditStatus: false,
				canEditAssignment: false
			}
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		const request = {
			title: 'Completely Updated',
			description: 'All fields changed',
			category: 2,
			priority: 4,
			status: 3,
			assignedToUserId: 300
		};

		const result = await updateTicket(123, request);

		expect(result).toEqual(mockResponse);
		expect(result.capabilities?.canEditTitle).toBe(false); // Resolved ticket
	});

	it('should throw formatted error for 400 validation error with single field', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
			title: 'One or more validation errors occurred.',
			status: 400,
			errors: {
				Title: ['Title cannot be empty']
			},
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 400,
			json: async () => mockErrorResponse
		});

		expect.assertions(2);
		try {
			await updateTicket(123, { title: '' });
		} catch (err: any) {
			expect(err?.message).toBe('Validation failed');
			expect(err?.fieldErrors).toEqual({ Title: ['Title cannot be empty'] });
		}
	});

	it('should throw formatted error for 400 validation error with multiple fields', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
			title: 'One or more validation errors occurred.',
			status: 400,
			errors: {
				Title: ['Title is required', 'Title must be at least 3 characters'],
				Description: ['Description cannot be empty']
			},
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 400,
			json: async () => mockErrorResponse
		});

		expect.assertions(2);
		try {
			await updateTicket(123, { title: '', description: '' });
		} catch (err: any) {
			expect(err?.message).toBe('Validation failed');
			expect(err?.fieldErrors).toEqual({
				Title: ['Title is required', 'Title must be at least 3 characters'],
				Description: ['Description cannot be empty']
			});
		}
	});

	it('should throw permission error for 403 Forbidden', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.4',
			title: 'Forbidden',
			status: 403,
			detail: 'You do not have permission to edit this field',
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 403,
			json: async () => mockErrorResponse
		});

		await expect(updateTicket(123, { priority: 4 })).rejects.toThrow(
			'You do not have permission to edit this field'
		);
	});

	it('should throw not found error for 404', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.5',
			title: 'Not Found',
			status: 404,
			detail: 'Ticket not found',
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 404,
			statusText: 'Not Found',
			json: async () => mockErrorResponse
		});

		expect.assertions(2);
		try {
			await updateTicket(999, { title: 'New Title' });
		} catch (err: any) {
			expect(err?.message).toBe('Update ticket failed: 404 Not Found');
			expect(err?.detail).toBe('Ticket not found');
		}
	});

	it('should throw generic error for 500 server error', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.6.1',
			title: 'Internal Server Error',
			status: 500,
			detail: 'An unexpected error occurred',
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 500,
			statusText: 'Internal Server Error',
			json: async () => mockErrorResponse
		});

		expect.assertions(2);
		try {
			await updateTicket(123, { title: 'New Title' });
		} catch (err: any) {
			expect(err?.message).toBe('Update ticket failed: 500 Internal Server Error');
			expect(err?.detail).toBe('An unexpected error occurred');
		}
	});

	it('should throw error with default message if detail is missing', async () => {
		const mockErrorResponse = {
			type: 'https://tools.ietf.org/html/rfc9110#section-15.5.1',
			title: 'Bad Request',
			status: 400,
			traceId: '00-trace-id-00'
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 400,
			json: async () => mockErrorResponse
		});

		expect.assertions(2);
		try {
			await updateTicket(123, { title: 'New Title' });
		} catch (err: any) {
			expect(err?.message).toBe('Validation failed');
			expect(err?.code).toBe('VALIDATION_FAILED');
		}
	});

	it('should handle network errors', async () => {
		(fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

		await expect(updateTicket(123, { title: 'New Title' })).rejects.toThrow('Network error');
	});

	it('should handle empty request body', async () => {
		const mockResponse = {
			ticketId: 123,
			title: 'Original Title',
			description: 'Original Description',
			category: 1,
			categoryName: 'IT',
			priority: 2,
			priorityName: 'Medium',
			status: 1,
			statusName: 'Open',
			createdBy: 100,
			createdByName: 'Creator',
			createdAt: '2025-01-01T10:00:00Z',
			updatedAt: '2025-01-01T10:00:00Z',
			assignedToId: null,
			assignedToName: null,
			capabilities: {
				canEditTitle: true,
				canEditDescription: true,
				canEditCategory: true,
				canEditPriority: true,
				canEditStatus: true,
				canEditAssignment: true
			}
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		const result = await updateTicket(123, {});

		expect(fetch).toHaveBeenCalledWith(
			'/api/tickets/123',
			expect.objectContaining({
				method: 'PATCH',
				headers: { 'Content-Type': 'application/json' },
				body: JSON.stringify({}),
				credentials: 'include'
			})
		);

		expect(result).toEqual(mockResponse);
	});

	it('should handle unassigning a ticket', async () => {
		const mockResponse = {
			ticketId: 123,
			title: 'Test Ticket',
			description: 'Test Description',
			category: 1,
			categoryName: 'IT',
			priority: 2,
			priorityName: 'Medium',
			status: 1,
			statusName: 'Open',
			createdBy: 100,
			createdByName: 'Creator',
			createdAt: '2025-01-01T10:00:00Z',
			updatedAt: '2025-01-04T14:45:00Z',
			assignedToId: null,
			assignedToName: null,
			capabilities: {
				canEditTitle: true,
				canEditDescription: true,
				canEditCategory: true,
				canEditPriority: true,
				canEditStatus: true,
				canEditAssignment: true
			}
		};

		(fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		const result = await updateTicket(123, { assignedToUserId: null });

		expect(result.assignedToId).toBeNull();
		expect(result.assignedToName).toBeNull();
	});
});
