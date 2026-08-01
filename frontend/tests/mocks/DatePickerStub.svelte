<script lang="ts">
  let {
    value = $bindable<string | undefined>(undefined),
    id,
    name,
    placeholder
  } = $props();

  function handleInput(event: Event) {
    const target = event.currentTarget as HTMLInputElement;
    value = target.value ? target.value : undefined;
  }
</script>

<input
  type="text"
  class="date-picker-stub"
  {id}
  {name}
  value={value ?? ''}
  placeholder={placeholder}
  on:input={handleInput}
/>
