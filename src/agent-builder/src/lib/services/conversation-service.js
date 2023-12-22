import { endpoints } from './api-endpoints.js';
import { setAuthorization } from '$lib/helpers/http';
import axios from 'axios';

/**
 * New conversation
 * @param {string} agentId 
 * @returns {Promise<import('$types').ConversationModel>}
 */
export async function newConversation(agentId) {
    setAuthorization();
    let url = endpoints.conversationInitUrl.replace("{agentId}", agentId);
    const response = await axios.post(url, {});
    return response.data;
}

/**
 * Get conversation list
 * @param {import('$types').ConversationFilter} filter
 * @returns {Promise<import('$types').PagedItems<import('$types').ConversationModel>>}
 */
export async function getConversations(filter) {
    setAuthorization();
    let url = endpoints.conversationsUrl;
    const response = await axios.get(url, {
        params: filter
    });
    return response.data;
}

/**
 * Delete conversation
 * @param {string} conversationId 
 */
export async function deleteConversation(conversationId) {
    setAuthorization();
    let url = endpoints.conversationDeletionUrl.replace("{conversationId}", conversationId);
    const response = await axios.delete(url);
    return response.data;
}

/**
 * Get dialog history
 * @param {string} conversationId 
 * @returns {Promise<import('$types').ChatResponseModel[]>}
 */
export async function GetDialogs(conversationId) {
    setAuthorization();
    let url = endpoints.dialogsUrl.replace("{conversationId}", conversationId);
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
    let url = endpoints.conversationMessageUrl.replace("{agentId}", agentId)
        .replace("{conversationId}", conversationId);
    const response = await axios.post(url, {
        "text": message
    });
    return response.data;
}
