/**
 * File type utilities for user-friendly file type display and preview detection
 */

export type FileType = 'image' | 'pdf' | 'document' | 'archive' | 'text' | 'unknown';

export interface FileTypeInfo {
	type: FileType;
	i18nKey: string;
	previewable: boolean;
}

/**
 * Map MIME type to user-friendly file type category
 */
export function getFileTypeInfo(mimeType: string): FileTypeInfo {
	const mime = mimeType.toLowerCase();

	// Images
	if (mime.startsWith('image/')) {
		const previewable = [
			'image/png',
			'image/jpeg',
			'image/jpg',
			'image/gif',
			'image/webp'
		].includes(mime);

		return {
			type: 'image',
			i18nKey: 'file_type_image',
			previewable
		};
	}

	// PDF
	if (mime === 'application/pdf') {
		return {
			type: 'pdf',
			i18nKey: 'file_type_pdf',
			previewable: true
		};
	}

	// Documents
	if (
		mime.includes('word') ||
		mime.includes('document') ||
		mime.includes('msword') ||
		mime.includes('officedocument')
	) {
		return {
			type: 'document',
			i18nKey: 'file_type_document',
			previewable: false
		};
	}

	// Archives
	if (
		mime === 'application/zip' ||
		mime === 'application/x-zip-compressed' ||
		mime === 'application/x-rar-compressed' ||
		mime === 'application/x-7z-compressed' ||
		mime === 'application/gzip'
	) {
		return {
			type: 'archive',
			i18nKey: 'file_type_archive',
			previewable: false
		};
	}

	// Text files
	if (mime.startsWith('text/')) {
		return {
			type: 'text',
			i18nKey: 'file_type_text',
			previewable: false
		};
	}

	// Unknown
	return {
		type: 'unknown',
		i18nKey: 'file_type_unknown',
		previewable: false
	};
}

/**
 * Check if a file can be previewed based on MIME type
 */
export function isPreviewable(mimeType: string): boolean {
	return getFileTypeInfo(mimeType).previewable;
}

/**
 * Get file extension from filename
 */
export function getFileExtension(filename: string): string {
	const parts = filename.split('.');
	return parts.length > 1 ? parts[parts.length - 1].toUpperCase() : '';
}
