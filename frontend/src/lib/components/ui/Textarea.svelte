<script lang="ts">
	/**
	 * Generic Textarea component - reusable form primitive
	 * 
	 * @example
	 * <Textarea bind:value={description} placeholder="Enter description" rows={5} />
	 * <Textarea bind:value={comment} error="Comment is required" />
	 */

	interface TextareaProps {
		value?: string;
		placeholder?: string;
		disabled?: boolean;
		readonly?: boolean;
		required?: boolean;
		class?: string;
		error?: string | string[];
		id?: string;
		name?: string;
		rows?: number;
		maxlength?: number;
	}

	let {
		value = $bindable(''),
		placeholder,
		disabled = false,
		readonly = false,
		required = false,
		class: className = '',
		error,
		id,
		name,
		rows = 3,
		maxlength
	}: TextareaProps = $props();

	const textareaClass = $derived(['form-control', error ? 'is-invalid' : '', className].filter(Boolean).join(' '));
</script>

<textarea
	bind:value
	{placeholder}
	{disabled}
	{readonly}
	{required}
	{id}
	{name}
	{rows}
	{maxlength}
	class={textareaClass}
	aria-invalid={error ? 'true' : undefined}
	aria-describedby={error && id ? `${id}-error` : undefined}
></textarea>

{#if error}
	<div id={id ? `${id}-error` : undefined} class="invalid-feedback">
		{#if Array.isArray(error)}
			{#each error as message}
				<div>{message}</div>
			{/each}
		{:else}
			{error}
		{/if}
	</div>
{/if}

<style>
	.form-control {
		display: block;
		width: 100%;
		padding: 0.375rem 0.75rem;
		font-size: 1rem;
		font-weight: 400;
		line-height: 1.5;
		color: #212529;
		background-color: #fff;
		background-clip: padding-box;
		border: 1px solid #ced4da;
		border-radius: 0.25rem;
		transition: border-color 0.15s ease-in-out, box-shadow 0.15s ease-in-out;
		resize: vertical;
	}

	.form-control:focus {
		color: #212529;
		background-color: #fff;
		border-color: var(--ironpack-border);
		outline: 0;
		box-shadow: 0 0 0 0.25rem 	rgba(var(--ironpack-border-rgb), 0.2);
	}

	.form-control:disabled,
	.form-control[readonly] {
		background-color: #e9ecef;
		opacity: 1;
	}

	.form-control.is-invalid {
		border-color: #dc3545;
	}

	.form-control.is-invalid:focus {
		border-color: #dc3545;
		box-shadow: 0 0 0 0.25rem rgba(220, 53, 69, 0.25);
	}

	.invalid-feedback {
		display: block;
		width: 100%;
		margin-top: 0.25rem;
		font-size: 0.875em;
		color: #dc3545;
	}
</style>
