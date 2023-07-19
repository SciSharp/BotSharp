using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    Task<Conversation> NewConversation(Conversation conversation);
    Task<Conversation> GetConversation(string id);
    Task<List<Conversation>> GetConversations();
    Task DeleteConversation(string id);
    Task<string> SendMessage(string agentId, string conversationId, RoleDialogModel lastDalog);
    Task<string> SendMessage(string agentId, string conversationId, List<RoleDialogModel> wholeDialogs);
    List<RoleDialogModel> GetDialogHistory(string agentId, string conversationId);
    Task CleanHistory(string agentId);
}
