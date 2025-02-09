using BotSharp.Abstraction.Conversations;

namespace BotSharp.Plugin.Dashboard.Hooks;

public class StatsConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;
    public StatsConversationHook(IServiceProvider services)
    {
        _services = services;
    }

    public override async Task OnConversationInitialized(Conversation conversation)
    {
    }
}
