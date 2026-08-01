export type EnsureState<T> = {
	getValue: () => T | null;
	setValue: (value: T) => void;
	getLoading: () => boolean;
	setLoading: (loading: boolean) => void;
	setError: (error: string | null) => void;
	loader: () => Promise<T>;
	errorFallback: () => string;
};

/**
 * Creates an idempotent "ensure" function for lazy-loading a resource.
 *
 * Contract:
 * - Only loads if value is null and not already loading
 * - Stores error message on failure
 */
export function createEnsure<T>(state: EnsureState<T>) {
	return async function ensure(): Promise<void> {
		if (state.getValue() !== null) return;
		if (state.getLoading()) return;

		state.setLoading(true);
		state.setError(null);
		try {
			const value = await state.loader();
			state.setValue(value);
		} catch (e: any) {
			const msg = e?.message ?? state.errorFallback();
			state.setError(msg);
		} finally {
			state.setLoading(false);
		}
	};
}
