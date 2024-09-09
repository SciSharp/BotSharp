using BotSharp.Abstraction.Loggers.Models;
using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    IConversationStateService States { get; }
    string ConversationId { get; }
    Task<Conversation> NewConversation(Conversation conversation);
    void SetConversationId(string conversationId, List<MessageState> states);
    Task<Conversation> GetConversation(string id);
    Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter);
    Task<Conversation> UpdateConversationTitle(string id, string title);
    Task<List<Conversation>> GetLastConversations();
    Task<List<string>> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds);
    Task<bool> DeleteConversations(IEnumerable<string> ids);

    /// <summary>
    /// Truncate conversation
    /// </summary>
    /// <param name="conversationId">Target conversation id</param>
    /// <param name="messageId">Target message id to delete</param>
    /// <param name="newMessageId">If not null, delete messages while input a new message; otherwise delete messages only</param>
    /// <returns></returns>
    Task<bool> TruncateConversation(string conversationId, string messageId, string? newMessageId = null);
    Task<List<ContentLogOutputModel>> GetConversationContentLogs(string conversationId);
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
        PostbackMessageModel? replyMessage,
        Func<RoleDialogModel, Task> onResponseReceived, 
        Func<RoleDialogModel, Task> onFunctionExecuting,
        Func<RoleDialogModel, Task> onFunctionExecuted);

    List<RoleDialogModel> GetDialogHistory(int lastCount = 100, bool fromBreakpoint = true);
    Task CleanHistory(string agentId);

    /// <summary>
    /// Use this feature when you want to hide some context from LLM.
    /// </summary>
    /// <param name="resetStates">Whether to reset all states</param>
    /// <param name="reason">Append user init words</param>
    /// <param name="excludedStates"></param>
    /// <returns></returns>
    Task UpdateBreakpoint(bool resetStates = false, string? reason = null, params string[] excludedStates);

    Task<string> GetConversationSummary(IEnumerable<string> conversationId);

    Task<Conversation> GetConversationRecordOrCreateNew(string agentId);

    bool IsConversationMode();
}
