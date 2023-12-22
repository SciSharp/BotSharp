<svelte:head>
	<!-- jsvectormap -->
	<script src="https://cdn.jsdelivr.net/npm/jsvectormap"></script>
	<script src="https://cdn.jsdelivr.net/npm/jsvectormap/dist/maps/world.js"></script>
</svelte:head>

<script>
    import { setAuthorization } from '$lib/helpers/http';
    import { getUserStore } from '$lib/helpers/store.js';

    let user = getUserStore();
	let token = user.token;
	setAuthorization(token);

	import VerticalLayout from '../VerticalLayout/Index.svelte';
	let component = VerticalLayout;
</script>

<svelte:component this={component}>
	<slot />
</svelte:component>