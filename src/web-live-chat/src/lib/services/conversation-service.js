import { 
        conversationInitUrl, 
        conversationMessageUrl,
        dialogsUrl, 
        conversationReadyUrl,
    } from './api-endpoints.js';

import axios from 'axios';

/**
 * New conversation
 * @param {string} agentId 
 * @returns {Promise<import('$typedefs').ConversationModel>}
 */
export async function newConversation(agentId) {
    let url = conversationInitUrl.replace("{agentId}", agentId);
    const response = await axios.post(url, {});
    return response.data;
}

/**
 * Coversation is ready
 * @param {string} conversationId 
 */
export async function conversationReady(conversationId) {
    let url = conversationReadyUrl.replace("{conversationId}", conversationId);
    const response = await axios.patch(url, {});
    return response.data;
}

/**
 * Get dialog history
 * @param {string} conversationId 
 * @returns {Promise<import('$typedefs').ChatResponseModel[]>}
 */
export async function GetDialogs(conversationId) {
    let url = dialogsUrl.replace("{conversationId}", conversationId);
    const response = await axios.get(url);
    return response.data;
}

 // send a message to the hub
/**
 * @param {string} agentId The agent id
 * @param {string} conversationId The conversation id
 * @param {string} message The text message sent to CSR
 */
export async function sendMessageToHub(agentId, conversationId, message) {
    let url = conversationMessageUrl.replace("{agentId}", agentId)
        .replace("{conversationId}", conversationId);
    const response = await axios.post(url, {
        "text": message
    });
    return response.data;
}
