namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void InitStorage(string conversationId);
    void Append(string conversationId, string agentId, RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogs(string conversationId);
}
