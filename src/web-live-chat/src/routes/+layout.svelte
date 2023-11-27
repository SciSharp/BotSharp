<script>
	import VerticalLayout from './VerticalLayout/Index.svelte';
	let component = VerticalLayout;
</script>

<svelte:component this={component}>
	<slot />
</svelte:component>

<style lang="scss">
	@import '$lib/scss/bootstrap.scss';
	@import '$lib/scss/app.scss';
	@import '$lib/scss/icons.scss';
</style>