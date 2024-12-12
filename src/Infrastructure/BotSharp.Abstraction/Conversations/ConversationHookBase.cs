namespace BotSharp.Abstraction.Conversations;

public abstract class ConversationHookBase : IConversationHook
{
    public Agent Agent { get; private set; }

    public Conversation Conversation { get; private set; }

    public List<RoleDialogModel> Dialogs { get; private set; }

    public int Priority { get; protected set; } = 0;

    public IConversationHook SetAgent(Agent agent)
    {
        Agent = agent;
        return this;
    }

    public IConversationHook SetConversation(Conversation conversation)
    {
        Conversation = conversation;
        return this;
    }

    public virtual Task OnStateLoaded(ConversationState state)
        => Task.CompletedTask;

    public virtual Task OnStateChanged(StateChangeModel stateChange)
        => Task.CompletedTask;

    public virtual Task OnDialogRecordLoaded(RoleDialogModel dialog)
        => Task.CompletedTask;

    public virtual Task OnDialogsLoaded(List<RoleDialogModel> dialogs)
    {
        Dialogs = dialogs;
        return Task.CompletedTask;
    }

    public virtual Task OnConversationEnding(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnNewTaskDetected(RoleDialogModel message, string reason)
        => Task.CompletedTask;

    public virtual Task OnTaskCompleted(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnHumanInterventionNeeded(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnFunctionExecuting(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnFunctionExecuted(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnMessageReceived(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
        => Task.CompletedTask;

    public virtual Task OnResponseGenerated(RoleDialogModel message)
        => Task.CompletedTask;

    public virtual Task OnConversationInitialized(Conversation conversation)
        => Task.CompletedTask;

    public virtual Task OnUserAgentConnectedInitially(Conversation conversation)
        => Task.CompletedTask;

    public virtual Task OnMessageDeleted(string conversationId, string messageId)
        => Task.CompletedTask;

    public virtual Task OnBreakpointUpdated(string conversationId, bool resetStates)
        => Task.CompletedTask;

    public virtual Task OnNotificationGenerated(RoleDialogModel message)
        => Task.CompletedTask;
}
