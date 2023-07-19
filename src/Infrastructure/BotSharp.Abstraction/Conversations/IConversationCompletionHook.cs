using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationCompletionHook
{
    Agent Agent { get; }
    Conversation Conversation { get; }
    List<RoleDialogModel> Dialogs { get; }
    IConversationCompletionHook SetContexts(Agent agent, Conversation conversation, List<RoleDialogModel> dialogs);
    Task BeforeCompletion();
    Task<string> AfterCompletion(string response);
}
