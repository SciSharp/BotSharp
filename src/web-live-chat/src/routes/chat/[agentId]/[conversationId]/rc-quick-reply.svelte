<script>
	/** @type {import('$typedefs').QuickReplyMessage} */
	export let message;

</script>

<span>{message.text}</span>
<div class="fixed-bottom p-2 text-center" style="margin-bottom: 10vh;">
{#each message.quick_replies as reply}
<button class="btn btn-secondary btn-rounded btn-sm m-1">{reply.title}</button>
{/each}
</div>