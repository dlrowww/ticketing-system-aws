import { describe, it, expect, vi } from 'vitest';
import { render, fireEvent } from '@testing-library/svelte';
import TableToolbar from '$lib/components/tables/base/TableToolbar.svelte';

vi.mock('$lib/i18n', () => ({
	getMessage: (key: string) => key
}));

vi.mock('$lib/components/ui/DatePicker.svelte', async () => ({
	default: (await import('../../../mocks/DatePickerStub.svelte')).default
}));

describe('TableToolbar date range normalization', () => {
	it('aligns dateFrom when dateTo is moved earlier', async () => {
		const { getByLabelText } = render(TableToolbar as any);

		const fromInput = getByLabelText('date_from') as HTMLInputElement;
		const toInput = getByLabelText('date_to') as HTMLInputElement;

		await fireEvent.input(fromInput, { target: { value: '2025-02-01' } });
		await fireEvent.input(toInput, { target: { value: '2025-01-01' } });

		expect(fromInput.value).toBe('2025-01-01');
		expect(toInput.value).toBe('2025-01-01');
	});

	it('aligns dateTo when dateFrom is moved later', async () => {
		const { getByLabelText } = render(TableToolbar as any, {
			props: { dateFrom: '2025-01-01', dateTo: '2025-01-10' }
		});

		const fromInput = getByLabelText('date_from') as HTMLInputElement;
		const toInput = getByLabelText('date_to') as HTMLInputElement;

		await fireEvent.input(fromInput, { target: { value: '2025-02-01' } });

		expect(fromInput.value).toBe('2025-02-01');
		expect(toInput.value).toBe('2025-02-01');
	});
});
