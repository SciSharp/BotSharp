namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void InitStorage(string conversationId);
    void Append(string conversationId, RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogs(string conversationId);
}
