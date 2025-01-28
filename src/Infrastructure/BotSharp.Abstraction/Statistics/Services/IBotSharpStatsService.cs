using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Abstraction.Statistics.Services;

public interface IBotSharpStatsService
{
    bool UpdateLlmCost(BotSharpStats stats);
    bool UpdateAgentCall(BotSharpStats stats);
}
