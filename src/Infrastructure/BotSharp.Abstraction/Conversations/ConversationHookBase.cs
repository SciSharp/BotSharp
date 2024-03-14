namespace BotSharp.Abstraction.Conversations;

public abstract class ConversationHookBase : IConversationHook
{
    protected Agent _agent;
    public Agent Agent => _agent;

    protected Conversation _conversation;
    public Conversation Conversation => _conversation;

    protected List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    protected int _priority = 0;
    public int Priority => _priority;

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
        => Task.CompletedTask;

    public virtual Task OnStateChanged(string name, string preValue, string currentValue)
        => Task.CompletedTask;

    public virtual Task OnDialogRecordLoaded(RoleDialogModel dialog)
        => Task.CompletedTask;

    public virtual Task OnDialogsLoaded(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
        return Task.CompletedTask;
    }

    public virtual Task OnConversationEnding(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnCurrentTaskEnding(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnHumanInterventionNeeded(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnFunctionExecuting(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnFunctionExecuted(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnMessageReceived(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnResponseGenerated(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnConversationInitialized(Conversation conversation)
        => Task.CompletedTask;

    public virtual Task OnUserAgentConnectedInitially(Conversation conversation)
        => Task.CompletedTask;

    public virtual Task OnMessageDeleted(string conversationId, string messageId)
        => Task.CompletedTask;
}
