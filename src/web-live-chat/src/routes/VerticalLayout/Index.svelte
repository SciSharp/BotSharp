<script>
	import { browser } from '$app/environment';
	import { onMount } from 'svelte';

	onMount(() => {
		if (browser) {
			document.body.setAttribute('data-layout', 'vertical');
		}
	});
</script>

<slot />
