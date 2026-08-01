import { TicketStatus as TS, type TicketStatus } from '$lib/types/enums';

export type StatusPaletteEntry = {
	className: string;
	background: string;
	foreground: string;
	border: string;
};

const palette: Record<TicketStatus, StatusPaletteEntry> = {
	[TS.New]: {
		className: 'bg-status-new',
		background: '#f1f3f5',
		foreground: '#495057',
		border: '#adb5bd'
	},
	[TS.Open]: {
		className: 'bg-status-open',
		background: '#e7f5ff',
		foreground: '#1c7ed6',
		border: '#1c7ed6'
	},
	[TS.InProcess]: {
		className: 'bg-status-inprocess',
		background: '#fff9db',
		foreground: '#f59f00',
		border: '#f59f00'
	},
	[TS.Resolved]: {
		className: 'bg-status-resolved',
		background: '#ebfbee',
		foreground: '#2f9e44',
		border: '#2f9e44'
	},
	[TS.Cancelled]: {
		className: 'bg-status-cancelled',
		background: '#ffe3e3',
		foreground: '#e03131',
		border: '#e03131'
	},
	[TS.Postponed]: {
		className: 'bg-status-postponed',
		background: '#fff3bf',
		foreground: '#f08c00',
		border: '#f08c00'
	},
	[TS.Returned]: {
		className: 'bg-status-returned',
		background: '#f3f0ff',
		foreground: '#6741d9',
		border: '#6741d9'
	}
};

const fallback: StatusPaletteEntry = {
	className: 'bg-status-unknown',
	background: '#f1f3f5',
	foreground: '#6c757d',
	border: '#6c757d'
};

export function getStatusPalette(status: TicketStatus): StatusPaletteEntry {
	return palette[status] ?? fallback;
}

export function getStatusBackground(status: TicketStatus): string {
	return getStatusPalette(status).background;
}

export function getStatusBorderColor(status: TicketStatus): string {
	return getStatusPalette(status).border;
}

export function getStatusClassName(status: TicketStatus): string {
	return getStatusPalette(status).className;
}
