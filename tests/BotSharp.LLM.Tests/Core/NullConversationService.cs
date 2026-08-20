using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.Models;
using BotSharp.Abstraction.Repositories.Filters;
using BotSharp.Abstraction.Utilities;

namespace BotSharp.Plugin.Google.Core
{
    /// <summary>
    /// Minimal IConversationService test stub. Only ConversationId is exercised by the
    /// streaming code path; the rest satisfy the interface and are not called in tests.
    /// </summary>
    public class NullConversationService : IConversationService
    {
        private readonly IConversationStateService _states;

        public NullConversationService(IConversationStateService states)
        {
            _states = states;
        }

        public IConversationStateService States => _states;
        public string ConversationId => "test-conversation-id";

        public Task<Conversation> NewConversation(Conversation conversation) => Task.FromResult(conversation);
        public Task SetConversationId(string conversationId, List<MessageState> states, bool isReadOnly = false) => Task.CompletedTask;
        public Task<Conversation> GetConversation(string id, bool isLoadStates = false) => Task.FromResult(new Conversation());
        public Task<PagedItems<Conversation>> GetConversations(ConversationFilter filter) => Task.FromResult(new PagedItems<Conversation>());
        public Task<bool> UpdateConversationTitle(string id, string title) => Task.FromResult(true);
        public Task<bool> UpdateConversationTitleAlias(string id, string titleAlias) => Task.FromResult(true);
        public Task<bool> UpdateConversationTags(string conversationId, List<string> toAddTags, List<string> toDeleteTags) => Task.FromResult(true);
        public Task<bool> UpdateConversationMessage(string conversationId, UpdateMessageRequest request) => Task.FromResult(true);
        public Task<List<Conversation>> GetLastConversations() => Task.FromResult(new List<Conversation>());
        public Task<List<string>> GetIdleConversations(int batchSize, int messageLimit, int bufferHours, IEnumerable<string> excludeAgentIds) => Task.FromResult(new List<string>());
        public Task<bool> DeleteConversations(IEnumerable<string> ids) => Task.FromResult(true);
        public Task<bool> TruncateConversation(string conversationId, string messageId, string? newMessageId = null) => Task.FromResult(true);
        public Task<bool> SendMessage(string agentId, RoleDialogModel message, PostbackMessageModel? replyMessage, Func<RoleDialogModel, Task> onResponseReceived) => Task.FromResult(true);
        public Task<List<RoleDialogModel>> GetDialogHistory(int lastCount = 100, bool fromBreakpoint = true, IEnumerable<string>? includeMessageTypes = null, ConversationDialogFilter? filter = null) => Task.FromResult(new List<RoleDialogModel>());
        public Task CleanHistory(string agentId) => Task.CompletedTask;
        public Task UpdateBreakpoint(bool resetStates = false, string? reason = null, params string[] excludedStates) => Task.CompletedTask;
        public Task<string> GetConversationSummary(ConversationSummaryModel model) => Task.FromResult(string.Empty);
        public Task<Conversation> GetConversationRecordOrCreateNew(string agentId) => Task.FromResult(new Conversation());
        public bool IsConversationMode() => false;
        public Task SaveStates() => Task.CompletedTask;
        public Task<List<string>> GetConversationStateSearhKeys(ConversationStateKeysFilter filter) => Task.FromResult(new List<string>());
    }
}
