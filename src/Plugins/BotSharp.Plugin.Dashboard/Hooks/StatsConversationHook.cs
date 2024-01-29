using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins.Models;
using BotSharp.Abstraction.Repositories;

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
        var db = _services.GetRequiredService<IBotSharpRepository>();
        db.IncrementConversationCount();
    }
}
