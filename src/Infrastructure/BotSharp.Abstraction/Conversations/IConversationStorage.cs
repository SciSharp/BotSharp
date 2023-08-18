using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void InitStorage(string conversationId);
    void Append(string conversationId, Agent agent, RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogs(string conversationId);
}
