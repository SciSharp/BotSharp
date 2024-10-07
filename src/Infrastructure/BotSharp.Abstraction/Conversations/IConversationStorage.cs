namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void Append(string conversationId, RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogs(string conversationId);
}
