using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Abstraction.Statistics.Services;

public interface IBotSharpStatsService
{
    bool UpdateStats(string resourceKey, BotSharpStatsInput input);
}
