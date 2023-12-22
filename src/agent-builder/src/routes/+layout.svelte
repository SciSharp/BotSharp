<script>
	import { addMessages, init, getLocaleFromNavigator } from 'svelte-i18n';
	import en from '$lib/langs/en.json';

	addMessages('en', en);

	init({
		fallbackLocale: 'en',
		initialLocale: getLocaleFromNavigator()
	});

</script>
<slot />

<style lang="scss">
	@import '$lib/scss/bootstrap.scss';
	@import '$lib/scss/app.scss';
	@import '$lib/scss/icons.scss';
</style>