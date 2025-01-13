namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    void Append(string conversationId, RoleDialogModel dialog);
    void Append(string conversationId, IEnumerable<RoleDialogModel> dialogs);
    List<RoleDialogModel> GetDialogs(string conversationId);
}
