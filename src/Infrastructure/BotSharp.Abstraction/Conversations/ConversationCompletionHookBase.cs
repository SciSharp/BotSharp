using BotSharp.Abstraction.Conversations.Models;
using BotSharp.Abstraction.MLTasks;

namespace BotSharp.Abstraction.Conversations;

public abstract class ConversationCompletionHookBase : IConversationCompletionHook
{
    protected Agent _agent;
    public Agent Agent => _agent;

    protected Conversation _conversation;
    public Conversation Conversation => _conversation;

    protected List<RoleDialogModel> _dialogs;
    public List<RoleDialogModel> Dialogs => _dialogs;

    protected IChatCompletion _chatCompletion;
    public IChatCompletion ChatCompletion => _chatCompletion;

    public IConversationCompletionHook SetAgent(Agent agent)
    {
        _agent = agent;
        return this;
    }

    public IConversationCompletionHook SetConversation(Conversation conversation)
    {
        _conversation = conversation;
        return this;
    }

    public IConversationCompletionHook SetDialogs(List<RoleDialogModel> dialogs)
    {
        _dialogs = dialogs;
        return this;
    }

    public IConversationCompletionHook SetChatCompletion(IChatCompletion chatCompletion)
    {
        _chatCompletion = chatCompletion;
        return this;
    }

    public virtual Task OnStateLoaded(ConversationState state, Action<Agent>? onAgentSwitched = null)
    {
        return Task.CompletedTask;
    }

    public virtual Task BeforeCompletion()
    {
        return Task.CompletedTask;
    }

    public virtual Task OnFunctionExecuting(string name, string args)
    {
        return Task.CompletedTask;
    }

    public virtual Task AfterCompletion(RoleDialogModel message)
    {
        return Task.CompletedTask;
    }
}
