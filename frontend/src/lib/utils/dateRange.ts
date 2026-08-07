export type LastChangedField = 'from' | 'to' | null;

export function normalizeDateRange(
	from: string | undefined,
	to: string | undefined,
	lastChanged: LastChangedField
): { from: string | undefined; to: string | undefined } {
	if (!from || !to) {
		return { from, to };
	}

	if (from <= to) {
		return { from, to };
	}

	if (lastChanged === 'to') {
		return { from: to, to };
	}

	return { from, to: from };
}
