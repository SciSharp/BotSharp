<script>
    import { Container, Row, Col } from "sveltestrap";

    // This page is used to initialize a new conversation for client
    import { page } from '$app/stores';
    import { onMount } from 'svelte';
    import { getAgents } from '$lib/services/agent-service.js'

    const params = $page.params;
    let agentId = "undefined";
    /** @type {import('$typedefs').AgentModel[]} */
    let agents = [];

    onMount(async () => {
        agents = await getAgents();
        agentId = agents[0].id;
    });
</script>

<Container fluid>
    <Row>
        <div class="col-12">
            <div style="margin-top: 10vh; margin-left:10vw;">
                {#each agents as agent}
                <div>
                    <input
                        class="form-check-input m-1"
                        type="radio"
                        name="agents"
                        id={agent.id}
                        value={agent.id}
                        checked = {agentId == agent.id}
                        on:click={() => agentId = agent.id}
                    />
                    <label class="form-check-label" for={agent.id}>
                        {agent.name}
                    </label>  
                    <div class="mx-4">{agent.description}</div>
                </div>
                {/each}
            </div>    
        </div>
    </Row>
    <Row class="text-center">
        <Col>
            <p class="section-subtitle text-muted text-center pt-4 font-secondary">We craft digital, graphic and dimensional thinking, to create category leading brand experiences that have meaning and add a value for our clients.</p>
            <div class="d-flex justify-content-center">
                <a href="/chat/{agentId}" class="btn btn-primary">
                    <i class="mdi mdi-chat" />
                    <span>Start Conversation</span>
                </a>
            </div>
        </Col>
    </Row>
</Container>