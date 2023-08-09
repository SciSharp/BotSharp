using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    Task<Conversation> NewConversation(Conversation conversation);
    Task<Conversation> GetConversation(string id);
    Task<List<Conversation>> GetConversations();
    Task DeleteConversation(string id);
    Task<bool> SendMessage(string agentId, string conversationId, RoleDialogModel lastDalog, Func<RoleDialogModel, Task> onMessageReceived, Func<RoleDialogModel, Task> onFunctionExecuting);
    Task<bool> SendMessage(string agentId, string conversationId, List<RoleDialogModel> wholeDialogs, Func<RoleDialogModel, Task> onMessageReceived);
    List<RoleDialogModel> GetDialogHistory(string conversationId, int lastCount = 20);
    Task CleanHistory(string agentId);
}
