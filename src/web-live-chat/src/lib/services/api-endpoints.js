import { PUBLIC_API_HOST } from '$env/static/public';
const host = PUBLIC_API_HOST;

// user
export const tokenUrl = `${host}/token`;
export const myInfoUrl = `${host}/user/my`;
export const usrCreationUrl = `${host}/user`;

// plugin
export const pluginListUrl = `${host}/plugins`;

// agent
export const agentListUrl = `${host}/agents`;
export const agentDetailUrl = `${host}/agent/{id}`;

// conversation
export const conversationInitUrl = `${host}/conversation/{agentId}`;
export const conversationMessageUrl = `${host}/conversation/{agentId}/{conversationId}`;
export const conversationsUrl = `${host}/conversations/{agentId}`;
export const dialogsUrl = `${host}/conversation/{conversationId}/dialogs`;

// chathub
export const chatHubUrl = `${host}/chatHub`;
