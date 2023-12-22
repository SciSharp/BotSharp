<script>
	import { sendMessageToHub, GetDialogs } from '$lib/services/conversation-service.js';

	/** @type {import('$types').QuickReplyMessage} */
	export let message;
	/** @type {string} */
	export let agentId;
	/** @type {string} */
	export let conversationId;

	export const fn = {
		/**
		 * @param {string} payload
		 */
		onTextSubmitted: (payload) => {}
	}

	/**
	 * @param {string} payload
	 */	
	async function onQuickReplyClick(payload) {
		await sendMessageToHub(agentId, conversationId, payload);
		message.quick_replies = [];
	}
</script>

<span>{message.text}</span>
<div class="fixed-bottom p-2 text-center" style="margin-bottom: 10vh;">
{#each message.quick_replies as reply}
<button class="btn btn-secondary btn-rounded btn-sm m-1" on:click={() => onQuickReplyClick(reply.payload)}>{reply.title}</button>
{/each}
</div>