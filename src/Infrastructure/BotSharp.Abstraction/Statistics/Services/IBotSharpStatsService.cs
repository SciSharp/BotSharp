using BotSharp.Abstraction.Statistics.Models;

namespace BotSharp.Abstraction.Statistics.Services;

public interface IBotSharpStatsService
{
    Task<bool> UpdateStats(string @event, BotSharpStatsDelta delta);
}
