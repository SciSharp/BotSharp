using BotSharp.Abstraction.Statistics.Enums;
using BotSharp.Abstraction.Statistics.Models;
using BotSharp.Abstraction.Statistics.Services;

namespace BotSharp.Logger.Hooks;

public class GlobalStatsConversationHook : IContentGeneratingHook
{
    private readonly IServiceProvider _services;

    public GlobalStatsConversationHook(
        IServiceProvider services)
    {
        _services = services;
    }

    public async Task AfterGenerated(RoleDialogModel message, TokenStatsModel tokenStats)
    {
        UpdateAgentCall(message);
        await Task.CompletedTask;
    }

    private void UpdateAgentCall(RoleDialogModel message)
    {
        // record agent call
        var globalStats = _services.GetRequiredService<IBotSharpStatsService>();

        var body = new BotSharpStatsInput
        {
            Metric = StatsMetric.AgentCall,
            Dimension = "agent",
            DimRefVal = message.CurrentAgentId ?? string.Empty,
            RecordTime = DateTime.UtcNow,
            IntervalType = StatsInterval.Day,
            Data = [
                new StatsKeyValuePair("agent_call_count", 1)
            ]
        };
        globalStats.UpdateStats("global-agent-call", body);
    }
}
