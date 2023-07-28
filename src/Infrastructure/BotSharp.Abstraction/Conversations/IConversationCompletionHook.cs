using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Abstraction.Conversations;

public interface IConversationCompletionHook
{
    Agent Agent { get; }
    IConversationCompletionHook SetAgent(Agent agent);

    Conversation Conversation { get; }
    IConversationCompletionHook SetConversation(Conversation conversation);

    List<RoleDialogModel> Dialogs { get; }
    IConversationCompletionHook SetDialogs(List<RoleDialogModel> dialogs);

    IChatCompletion ChatCompletion { get; }
    IConversationCompletionHook SetChatCompletion(IChatCompletion chatCompletion);
    
    Task BeforeCompletion();
    Task<string> OnFunctionExecution(string name, string args);
    Task AfterCompletion(RoleDialogModel message);
}
