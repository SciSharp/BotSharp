using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    IConversationStateService States { get; }
    string ConversationId { get; }
    Task<Conversation> NewConversation(Conversation conversation);
    Task SetConversationId(string conversationId, List<MessageState> states, bool isReadOnly = false);
    Task<Conversation> GetConversation(string id, bool isLoadStates = false);
    Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter);
    Task<bool> UpdateConversationTitle(string id, string title);
    Task<bool> UpdateConversationTitleAlias(string id, string titleAlias);
    Task<bool> UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags);
    Task<bool> UpdateConversationMessage(string conversationId, UpdateMessageRequest request);
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

    /// <summary>
    /// Send message to LLM
    /// </summary>
    /// <param name="agentId"></param>
    /// <param name="lastDialog"></param>
    /// <param name="replyMessage"></param>
    /// <param name="onResponseReceived">Received the response from AI Agent</param>
    /// <returns></returns>
    Task<bool> SendMessage(string agentId,
        RoleDialogModel message, 
        PostbackMessageModel? replyMessage,
        Func<RoleDialogModel, Task> onResponseReceived);

    Task<List<RoleDialogModel>> GetDialogHistory(int lastCount = 100, bool fromBreakpoint = true, IEnumerable<string>? includeMessageTypes = null, ConversationDialogFilter? filter = null);
    Task CleanHistory(string agentId);

    /// <summary>
    /// Use this feature when you want to hide some context from LLM.
    /// </summary>
    /// <param name="resetStates">Whether to reset all states</param>
    /// <param name="reason">Append user init words</param>
    /// <param name="excludedStates"></param>
    /// <returns></returns>
    Task UpdateBreakpoint(bool resetStates = false, string? reason = null, params string[] excludedStates);

    Task<string> GetConversationSummary(ConversationSummaryModel model);

    Task<Conversation> GetConversationRecordOrCreateNew(string agentId);

    bool IsConversationMode();

    Task SaveStates();

    Task<List<string>> GetConversationStateSearhKeys(ConversationStateKeysFilter filter);
}
