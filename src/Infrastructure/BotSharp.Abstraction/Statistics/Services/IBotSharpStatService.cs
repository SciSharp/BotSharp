using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Abstraction.Statistics.Services;

public interface IBotSharpStatService
{
    bool UpdateLlmCost(BotSharpStats stats);
    bool UpdateAgentCall(BotSharpStats stats);
}
