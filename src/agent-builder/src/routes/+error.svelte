<script>
    import { page } from '$app/stores';
    import Error500 from '$lib/common/errors/error-500.svelte'
</script>

{#if $page.status === 500 }
    <Error500 />
{:else}
    <h1>{$page.status} {$page.error.message}</h1>
    <p>Sorry, something went wrong on our end. Please try again later.</p>
{/if}