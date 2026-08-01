import type { DataTableConfig, TableColumn } from '$lib/types/table';
import type { TicketFileDto } from '$lib/types/tickets';
import { getFileTypeInfo } from '$lib/utils/fileTypes';
import { formatDateTimeForDetails } from '$lib/utils/dateTime';

function kb(n: number): string {
	return (n / 1024).toFixed(1);
}

export const attachmentsTableColumns: TableColumn<TicketFileDto>[] = [
	{
		key: 'originalName',
		label: 'file',
		width: '30%',
		sortable: false,
		formatter: (v: string) => v,
		cellClass: 'clickable-filename'
	},
	{
		key: 'contentType',
		label: 'file_type',
		width: '15%',
		sortable: false,
		formatter: (v: string) => {
			const typeInfo = getFileTypeInfo(v);
			return typeInfo.i18nKey; // Return i18n key, will be translated in DataTable
		}
	},
	{
		key: 'sizeBytes',
		label: 'file_size_kb',
		width: '10%',
		sortable: false,
		cellClass: 'text-end',
		formatter: (v: number) => kb(v)
	},
	{
		key: 'uploaderName',
		label: 'uploaded_by',
		width: '20%',
		sortable: false,
		formatter: (v: string | undefined) => v ?? '—'
	},
	{
		key: 'createdAt',
		label: 'uploaded_at',
		width: '25%',
		sortable: false,
		formatter: (v: string) => formatDateTimeForDetails(v)
	}
];

export const attachmentsTableConfig: DataTableConfig<TicketFileDto> = {
	keyField: 'ticketFileId',
	columns: attachmentsTableColumns,
	actions: [
		{
			id: 'preview',
			label: 'preview',
			icon: 'bi-eye',
			conditional: (row: TicketFileDto) => getFileTypeInfo(row.contentType).previewable
		},
		{
			id: 'download',
			label: 'download',
			icon: 'bi-download'
		}
	],
	enableSelection: false,
	enableSorting: false
};

