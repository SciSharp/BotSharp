<script>
	import Chat from './chat-box.svelte';
	import { page } from '$app/stores';
	import { onMount } from 'svelte';
	import { setAuthorization } from '$lib/helpers/http';
	import { myInfo } from '$lib/services/auth-service.js';
	import { getAgent } from '$lib/services/agent-service.js';

	const params = $page.params;

	/** @type {import('$typedefs').AgentModel} */
	let agent;

	/** @type {import('$typedefs').UserModel} */
	let currentUser;

    onMount(async () => {
		const token = $page.url.searchParams.get('token') ?? "unauthorized";
		setAuthorization(token);

		currentUser = await myInfo(token);

		// get agent profile
		let agentId = params.agentId;
		agent = await getAgent(agentId);
    });
</script>

<Chat currentUser={currentUser} agent={agent} />