import { agentListUrl, agentDetailUrl } from '$lib/services/api-endpoints.js';
import { setAuthorization } from '$lib/helpers/http';
import axios from 'axios';

/**
 * @returns {Promise<import('$typedefs').AgentModel[]>}
 */
export async function getAgents() {
    setAuthorization();
    const response = await axios.get(agentListUrl);
    return response.data;
}

/**
 * @param {string} id
 * @returns {Promise<import('$typedefs').AgentModel>}
 */
export async function getAgent(id) {
    setAuthorization();
    let url = agentDetailUrl.replace("{id}", id);
    const response = await axios.get(url);
    return response.data;
}
