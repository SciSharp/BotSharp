using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Abstraction.Statistics.Services;

public interface IBotSharpStatsService
{
    bool UpdateStats(string @event, BotSharpStatsDelta delta);
}
