import { Priority as P, type Priority } from '$lib/types/enums';

export type PriorityPaletteEntry = {
	background: string;
	foreground: string;
	border: string;
};

const palette: Record<Priority, PriorityPaletteEntry> = {
	[P.Low]: {
		background: '#d4edda',
		foreground: '#155724',
		border: '#c3e6cb'
	},
	[P.Medium]: {
		background: '#fff3cd',
		foreground: '#856404',
		border: '#ffeaa7'
	},
	[P.High]: {
		background: '#f8d7da',
		foreground: '#721c24',
		border: '#f5c6cb'
	},
	[P.Critical]: {
		background: '#dc3545',
		foreground: '#fff',
		border: '#bd2130'
	}
};

export function getPriorityBackground(priority: Priority): string {
	return palette[priority]?.background ?? '#e9ecef';
}

export function getPriorityForeground(priority: Priority): string {
	return palette[priority]?.foreground ?? '#495057';
}

export function getPriorityBorderColor(priority: Priority): string {
	return palette[priority]?.border ?? '#dee2e6';
}
