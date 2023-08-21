using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    Task<Conversation> NewConversation(Conversation conversation);
    Task<Conversation> GetConversation(string id);
    Task<List<Conversation>> GetConversations();
    Task DeleteConversation(string id);

    IChatCompletion GetChatCompletion();

    /// <summary>
    /// Send message to LLM
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="conversationId"></param>
    /// <param name="lastDalog"></param>
    /// <param name="onMessageReceived"></param>
    /// <param name="onFunctionExecuting">This delegate is useful when you want to report progress on UI</param>
    /// <param name="onFunctionExecuted">This delegate is useful when you want to report progress on UI</param>
    /// <returns></returns>
    Task<bool> SendMessage(string agentId, 
        string conversationId, 
        RoleDialogModel lastDalog, 
        Func<RoleDialogModel, Task> onMessageReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted);

    List<RoleDialogModel> GetDialogHistory(string conversationId, int lastCount = 20);
    Task CleanHistory(string agentId);
}
