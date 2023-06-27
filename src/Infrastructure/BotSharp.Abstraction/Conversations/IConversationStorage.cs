using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void InitStorage(string agentId, string conversationId);
    void Append(string agentId, string conversationId, RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogs(string agentId, string conversationId);
}
