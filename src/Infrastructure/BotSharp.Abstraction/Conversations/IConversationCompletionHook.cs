using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationCompletionHook
{
    Task BeforeCompletion(Agent agent, List<RoleDialogModel> conversations);
    Task<string> AfterCompletion(Agent agent, string response);
}
