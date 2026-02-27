using BotSharp.Abstraction.Repositories.Filters;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationStorage
{
    Task Append(string conversationId, RoleDialogModel dialog);
    Task Append(string conversationId, IEnumerable<RoleDialogModel> dialogs);
    Task<List<RoleDialogModel>> GetDialogs(string conversationId, ConversationDialogFilter? filter = null);
}
