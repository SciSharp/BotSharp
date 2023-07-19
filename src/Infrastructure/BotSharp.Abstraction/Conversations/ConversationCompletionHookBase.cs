using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public abstract class ConversationCompletionHookBase
{
    protected Agent _agent;
    public Agent Agent => _agent;

    protected Conversation _conversation;
    public Conversation Conversation => _conversation;

    protected List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public IConversationCompletionHook SetContexts(Agent agent, Conversation conversation, List<RoleDialogModel> dialogs)
    {
        _agent = agent;
        _conversation = conversation;
        _dialogs = dialogs;
        return this as IConversationCompletionHook;
    }
}
