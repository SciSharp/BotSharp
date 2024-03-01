using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    IConversationStateService States { get; }
    string ConversationId { get; }
    Task<Conversation> NewConversation(Conversation conversation);
    void SetConversationId(string conversationId, List<string> states);
    Task<Conversation> GetConversation(string id);
    Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter);
    Task<Conversation> UpdateConversationTitle(string id, string title);
    Task<List<Conversation>> GetLastConversations();
    Task<bool> DeleteConversation(string id);
    Task<bool> TruncateConversation(string conversationId, string messageId);
    Task<List<ConversationContentLogModel>> GetConversationContentLogs(string conversationId);
    Task<List<ConversationStateLogModel>> GetConversationStateLogs(string conversationId);

    /// <summary>
    /// Send message to LLM
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="lastDalog"></param>
    /// <param name="onResponseReceived">Received the response from AI Agent</param>
    /// <param name="onFunctionExecuting">This delegate is useful when you want to report progress on UI</param>
    /// <param name="onFunctionExecuted">This delegate is useful when you want to report progress on UI</param>
    /// <returns></returns>
    Task<bool> SendMessage(string agentId,
        RoleDialogModel lastDalog, 
        Func<RoleDialogModel, Task> onResponseReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted);

    List<RoleDialogModel> GetDialogHistory(int lastCount = 50);
    Task CleanHistory(string agentId);
}
