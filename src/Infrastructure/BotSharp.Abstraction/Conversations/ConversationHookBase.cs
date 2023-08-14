using BotSharp.Abstraction.Conversations.Models;

namespace BotSharp.Abstraction.Conversations;

public abstract class ConversationHookBase : IConversationHook
{
    protected Agent _agent;
    public Agent Agent => _agent;

    protected Conversation _conversation;
    public Conversation Conversation => _conversation;

    protected List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    public IConversationHook SetAgent(Agent agent)
    {
        _agent = agent;
        return this;
    }

    public IConversationHook SetConversation(Conversation conversation)
    {
        _conversation = conversation;
        return this;
    }

    public virtual Task OnStateLoaded(ConversationState state)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnStateChanged(string name, string preValue, string currentValue)
    {
        return Task.CompletedTask;
    }

    public virtual Task BeforeCompletion()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnFunctionExecuting(RoleDialogModel message)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnFunctionExecuted(RoleDialogModel message)
    {
        return Task.CompletedTask;
    }

    public virtual Task AfterCompletion(RoleDialogModel message)
    {
        return Task.CompletedTask;
    }

    public virtual Task OnDialogsLoaded(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
        return Task.CompletedTask;
    }
}
