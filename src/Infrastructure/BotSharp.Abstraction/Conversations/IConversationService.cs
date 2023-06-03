using BotSharp.Abstraction.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationService
{
    void AddDialog(RoleDialogModel dialog);
    List<RoleDialogModel> GetDialogHistory(string sessionId);
    void CleanHistory();
}
