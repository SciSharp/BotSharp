import { agentListUrl, agentDetailUrl } from '$lib/services/api-endpoints.js';
import axios from 'axios';

/**
 * @returns {Promise<import('$typedefs').AgentModel[]>}
 */
export async function getAgents() {
    const response = await axios.get(agentListUrl);
    return response.data;
}

/**
 * @param {string} id
 * @returns {Promise<import('$typedefs').AgentModel>}
 */
export async function getAgent(id) {
    let url = agentDetailUrl.replace("{id}", id);
    const response = await axios.get(url);
    return response.data;
}
