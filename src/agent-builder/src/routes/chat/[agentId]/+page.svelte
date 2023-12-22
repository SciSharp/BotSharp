<script>
    import { Container, Row, Col } from "sveltestrap";

    // This page is used to initialize a new conversation for client
    import { page } from '$app/stores';
    import { onMount } from 'svelte';
    import { newConversation } from '$lib/services/conversation-service.js';
    import { getToken, setToken } from '$lib/services/auth-service.js'
    import { setAuthorization } from '$lib/helpers/http';

    const params = $page.params;
    
    /** @type {import('$types').ConversationModel} */
    let conversation;
    let conversationId = "undefined";

    let agentId = params.agentId;

    onMount(async () => {
        if(!$page.url.searchParams.has('token')) {
            await getToken("guest@gmail.com", "123456", (token) => {
            });
        } else {
            let token = $page.url.searchParams.get('token') ?? "unauthorized";
            setToken(token);
        }

        // new conversation
        conversation = await newConversation(agentId);
        conversationId = conversation.id;

        window.location.href = `/chat/${agentId}/${conversationId}`;
    });
</script>

<Container fluid>
    <Row class="text-center">
        <Col style="padding: 50px;">
            <div class="spinner-grow text-primary m-1" role="status" style="padding: 50px;">
                <span class="sr-only">Loading...</span>
            </div>
            <h3>Initializing a conversation, wait a moment please...</h3>
            <a href={`/chat/${agentId}/${conversationId}`}>Click here if the browser doesn't redirect correctly.</a>
        </Col>
    </Row>
</Container>