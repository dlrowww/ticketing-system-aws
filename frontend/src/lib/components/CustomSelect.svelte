<script>
// @ts-nocheck

	let { options = [], value = '', onchange } = $props();

	let open = $state(false);
	let selected = $state(value || (options.length > 0 ? options[0].id : ''));
	let selectedValue = $state((options.find(item => item.id === selected) || {}).name || '');

	$effect(() => {
		// Only update selected if prop 'value' changes (ignore if set by user)
		if (value !== selected) {
			selected  = value;
		}
	});

	function handleSelect(optionId) {
		selected = optionId;
		open = false;
		onchange(optionId);
	}

	function toggleDropdown() {
		open = !open;
	}

	function handleBlur(event) {
		// Only close if the blur is not to the dropdown itself
		if (!event.relatedTarget || !event.currentTarget.contains(event.relatedTarget)) {
			open = false;
		}
	}
	//on:blur={handleBlur}
</script>

<div class="ironpack-custom-select" tabIndex="0" >
	<div
		class="select-display {open ? 'active' : ''}"
		onclick={toggleDropdown}
		aria-haspopup="listbox"
		aria-expanded={open}
	>
		<span>{selectedValue}</span>
		<svg
			class="chevron"
			width="18"
			height="18"
			fill="none"
			stroke="#b32c2c"
			stroke-width="2"
			viewBox="0 0 20 20"><path d="M5 8l5 5 5-5" /></svg
		>
	</div>
	<ul class="select-options" class:open tabindex="-1" role="listbox" aria-hidden={!open}>
		{#each options as option}
			<li
				class="select-option {option.id === selected ? 'selected' : ''}"
				onclick={() => handleSelect(option.id)}
				tabindex="0"
			>
				{option.name}
			</li>
		{/each}
	</ul>
</div>

<style>
	.ironpack-custom-select {
		position: relative;
		min-width: 96px;
		width: 94px;
		font-size: 0.8rem;
		z-index: 20;
		border-radius: 0.4em !important; /** 0.98em; */
	}
	.select-display {
		border: 2px solid #b32c2c;
		border-radius: 0.4em !important; /** 0.98em; */
		padding: 0.38em 2.2em 0.38em 0.8em;
		background: #fff;
		color: #b32c2c;
		font-weight: 600;
		cursor: pointer;
		transition:
			border-color 0.15s,
			box-shadow 0.15s;
		display: flex;
		align-items: center;
		position: relative;
		outline: none;
	}
	.select-display.active,
	.select-display:focus {
		border-color: #a02222;
		box-shadow: 0 0 0 2px rgba(179, 44, 44, 0.17);
	}
	.chevron {
		position: absolute;
		right: 1em;
		pointer-events: none;
		top: 50%;
		transform: translateY(-50%);
	}
	.select-options {
		position: absolute;
		left: 0;
		right: 0;
		margin: 0;
		padding: 0.2em 0;
		background: #fff;
		border: 2px solid #b32c2c;
		border-radius: 0.4em !important; /** 0.98em; */
		box-shadow: 0 8px 36px 0 rgba(179, 44, 44, 0.07);
		list-style: none;
		z-index: 99;
		opacity: 0;
		transform: translateY(-4px);
		visibility: hidden;
		pointer-events: none;
		transition: all 0.15s ease-in-out;
	}

	.select-options.open {
		opacity: 1;
		transform: translateY(0);
		visibility: visible;
		pointer-events: auto;
	}
	.select-option {
		padding: 0.5em 1.3em 0.5em 1.3em; /* More space left/right */
		cursor: pointer;
		border-radius: 0.3em; /* Subtle rounding */
		color: #b32c2c;
		background: #fff;
		transition:
			background 0.12s,
			color 0.12s;
		margin: 0.1em 0.1em;
		font-weight: 500;
	}

	.select-option.selected,
	.select-option:hover,
	.select-option:focus {
		background: #f8eaea;
		color: #a02222;
		outline: none;
	}
</style>
