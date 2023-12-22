export const host = 'http://localhost:5500';

export const endpoints = {
    // user
    tokenUrl: `${host}/token`,
    myInfoUrl: `${host}/user/my`,
    usrCreationUrl: `${host}/user`,
    
    // plugin
    pluginListUrl: `${host}/plugins`,
    
    // agent
    agentListUrl: `${host}/agents`,
    agentDetailUrl: `${host}/agent/{id}`,
    
    // conversation
    conversationInitUrl: `${host}/conversation/{agentId}`,
    conversationMessageUrl: `${host}/conversation/{agentId}/{conversationId}`,
    conversationsUrl: `${host}/conversations`,
    conversationCountUrl: `${host}/conversations/count`,
    conversationDeletionUrl: `${host}/conversation/{conversationId}`,
    dialogsUrl: `${host}/conversation/{conversationId}/dialogs`,
    
    // chathub 
    chatHubUrl: `${host}/chatHub`,
}

