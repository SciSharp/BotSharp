using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;
using BotSharp.Abstraction.Statistics.Services;

namespace BotSharp.Logger.Hooks;

public class GlobalStatsConversationHook : ConversationHookBase
{
    private readonly IServiceProvider _services;

    public GlobalStatsConversationHook(
        IServiceProvider services)
    {
        _services = services;
    }

    public override async Task OnMessageReceived(RoleDialogModel message)
    {
        UpdateAgentCall(message);
    }

    public override async Task OnPostbackMessageReceived(RoleDialogModel message, PostbackMessageModel replyMsg)
    {
        UpdateAgentCall(message);
    }

    private void UpdateAgentCall(RoleDialogModel message)
    {
        // record agent call
        var globalStats = _services.GetRequiredService<IBotSharpStatsService>();

        var body = new BotSharpStatsInput
        {
            Category = StatsCategory.AgentCall,
            Group = message.CurrentAgentId,
            Data = [
                new StatsKeyValuePair("agent_call_count", 1)
            ],
            RecordTime = DateTime.UtcNow
        };
        globalStats.UpdateStats("global-agent-call", body);
    }
}
