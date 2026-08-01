<script lang="ts">
	/**
	 * Form Error Display Component
	 * Shows validation errors in a styled alert box
	 * 
	 * @example
	 * <FormError message="Invalid input" />
	 * <FormError errors={['Error 1', 'Error 2']} />
	 */

	interface FormErrorProps {
		message?: string;
		errors?: string[];
		class?: string;
	}

	let {
		message,
		errors = [],
		class: className = ''
	}: FormErrorProps = $props();

	const allErrors = $derived(() => {
		const errs: string[] = [];
		if (message) errs.push(message);
		if (errors && errors.length > 0) errs.push(...errors);
		return errs;
	});

	const hasErrors = $derived(allErrors().length > 0);
	const classes = $derived(['form-error', className].filter(Boolean).join(' '));
</script>

{#if hasErrors}
	<div class={classes} role="alert">
		<i class="bi bi-exclamation-circle-fill"></i>
		{#if allErrors().length === 1}
			<span>{allErrors()[0]}</span>
		{:else}
			<ul>
				{#each allErrors() as err}
					<li>{err}</li>
				{/each}
			</ul>
		{/if}
	</div>
{/if}

<style>
	.form-error {
		display: flex;
		align-items: flex-start;
		gap: 0.5rem;
		padding: 0.75rem;
		margin-bottom: 1rem;
		background: #fde2e2;
		color: #8f1e1e;
		border: 1px solid #f5c2c2;
		border-radius: 0.375rem;
		font-size: 0.9rem;
	}

	.form-error i {
		flex-shrink: 0;
		font-size: 1.1rem;
		margin-top: 0.1rem;
	}

	.form-error ul {
		margin: 0;
		padding-left: 1.25rem;
	}

	.form-error li {
		margin-bottom: 0.25rem;
	}

	.form-error li:last-child {
		margin-bottom: 0;
	}
</style>
