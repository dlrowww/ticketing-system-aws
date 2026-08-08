import { describe, it, expect, vi, beforeEach } from 'vitest';
import { addTicketAttachments } from '$lib/services/Tickets';

// Mock fetch globally
global.fetch = vi.fn();

describe('Tickets Service - File Upload', () => {
	beforeEach(() => {
		vi.clearAllMocks();
	});

	it('should upload files successfully and return file metadata', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test content'], 'test.txt', { type: 'text/plain' })];

		const mockResponse = [
			{
				fileId: 1,
				ticketId: 1,
				fileName: 'test.txt',
				contentType: 'text/plain',
				sizeBytes: 1024,
				uploadedAt: '2026-01-06T22:00:00Z'
			}
		];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		// Act
		const result = await addTicketAttachments(ticketId, mockFiles);

		// Assert
		expect(result).toEqual(mockResponse);
		expect(global.fetch).toHaveBeenCalledTimes(1);

		const callArgs = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
		expect(callArgs[0]).toBe('/api/tickets/1/files');
		expect(callArgs[1].method).toBe('POST');
		expect(callArgs[1].credentials).toBe('include');

		// Verify FormData was created with correct files
		const formData = callArgs[1].body as FormData;
		expect(formData).toBeInstanceOf(FormData);
		expect(formData.getAll('files')).toHaveLength(1);
	});

	it('should handle multiple files upload', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [
			new File(['content 1'], 'file1.txt', { type: 'text/plain' }),
			new File(['content 2'], 'file2.pdf', { type: 'application/pdf' }),
			new File(['content 3'], 'file3.jpg', { type: 'image/jpeg' })
		];

		const mockResponse = [
			{
				fileId: 1,
				ticketId: 1,
				fileName: 'file1.txt',
				contentType: 'text/plain',
				sizeBytes: 512,
				uploadedAt: '2026-01-06T22:00:00Z'
			},
			{
				fileId: 2,
				ticketId: 1,
				fileName: 'file2.pdf',
				contentType: 'application/pdf',
				sizeBytes: 1024,
				uploadedAt: '2026-01-06T22:00:01Z'
			},
			{
				fileId: 3,
				ticketId: 1,
				fileName: 'file3.jpg',
				contentType: 'image/jpeg',
				sizeBytes: 2048,
				uploadedAt: '2026-01-06T22:00:02Z'
			}
		];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => mockResponse
		});

		// Act
		const result = await addTicketAttachments(ticketId, mockFiles);

		// Assert
		expect(result).toEqual(mockResponse);

		const formData = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0][1].body as FormData;
		expect(formData.getAll('files')).toHaveLength(3);
	});

	it('should throw error with user-friendly message on 401 Unauthorized', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 401
		});

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow(
			'You must be logged in to upload files'
		);
	});

	it('should throw error with user-friendly message on 403 Forbidden', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 403
		});

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow(
			'You do not have permission to upload files to this ticket'
		);
	});

	it('should throw error with user-friendly message on 404 Not Found', async () => {
		// Arrange
		const ticketId = 999;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 404
		});

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow('Ticket not found');
	});

	it('should throw error with user-friendly message on 413 Payload Too Large', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'huge-file.zip', { type: 'application/zip' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 413
		});

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow(
			'Files are too large. Maximum total size is 50 MB'
		);
	});

	it('should throw generic error for other error status codes', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: false,
			status: 500,
			statusText: 'Internal Server Error',
			json: async () => {
				throw new Error('Not JSON');
			}
		});

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow(
			'Failed to upload files. Please try again or contact support.'
		);
	});

	it('should handle network errors gracefully', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockRejectedValueOnce(new Error('Network error'));

		// Act & Assert
		await expect(addTicketAttachments(ticketId, mockFiles)).rejects.toThrow('Network error');
	});

	it('should include credentials in fetch request', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [new File(['test'], 'test.txt', { type: 'text/plain' })];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => []
		});

		// Act
		await addTicketAttachments(ticketId, mockFiles);

		// Assert
		const callArgs = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0];
		expect(callArgs[1].credentials).toBe('include');
	});

	it('should create FormData with correct field name', async () => {
		// Arrange
		const ticketId = 1;
		const mockFiles = [
			new File(['test 1'], 'file1.txt', { type: 'text/plain' }),
			new File(['test 2'], 'file2.txt', { type: 'text/plain' })
		];

		(global.fetch as ReturnType<typeof vi.fn>).mockResolvedValueOnce({
			ok: true,
			status: 200,
			json: async () => []
		});

		// Act
		await addTicketAttachments(ticketId, mockFiles);

		// Assert
		const formData = (global.fetch as ReturnType<typeof vi.fn>).mock.calls[0][1].body as FormData;
		const filesInFormData = formData.getAll('files');
		expect(filesInFormData).toHaveLength(2);
		expect(filesInFormData[0]).toBeInstanceOf(File);
		expect((filesInFormData[0] as File).name).toBe('file1.txt');
		expect((filesInFormData[1] as File).name).toBe('file2.txt');
	});
});
