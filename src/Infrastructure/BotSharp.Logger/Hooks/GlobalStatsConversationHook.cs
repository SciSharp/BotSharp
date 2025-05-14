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

        var metric = StatsMetric.AgentCall;
        var dim = "agent";
        var agentId = message.CurrentAgentId ?? string.Empty;
        var delta = new BotSharpStatsDelta
        {
            AgentId = agentId,
            RecordTime = DateTime.UtcNow,
            IntervalType = StatsInterval.Day,
            CountDelta = new()
            {
                AgentCallCountDelta = 1
            }
        };
        globalStats.UpdateStats($"global-{metric}-{dim}-{agentId}", delta);
    }
}
