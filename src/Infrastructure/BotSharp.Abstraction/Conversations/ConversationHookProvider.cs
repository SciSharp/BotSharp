namespace BotSharp.Abstraction.Conversations;

public class ConversationHookProvider
{
    public IEnumerable<IConversationHook> Hooks { get; }

    private readonly Lazy<IEnumerable<IConversationHook>> _hooksOrderByPriority;

    public IEnumerable<IConversationHook> HooksOrderByPriority
        => _hooksOrderByPriority.Value;

    public ConversationHookProvider(IEnumerable<IConversationHook> conversationHooks)
    {
        Hooks = conversationHooks;
        _hooksOrderByPriority = new Lazy<IEnumerable<IConversationHook>>(() =>
        {
            return conversationHooks.OrderBy(hook => hook.Priority).ToArray();
        });
    }
}