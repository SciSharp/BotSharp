using BotSharp.Abstraction.Statistics.Settings;

namespace BotSharp.Core.Statistics.Services;

public class BotSharpStatsService : IBotSharpStatsService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<BotSharpStatsService> _logger;
    private readonly StatisticsSettings _settings;

    public BotSharpStatsService(
        IServiceProvider services,
        ILogger<BotSharpStatsService> logger,
        StatisticsSettings settings)
    {
        _services = services;
        _logger = logger;
        _settings = settings;
    }


    public bool UpdateStats(string @event, BotSharpStatsDelta delta)
    {
        try
        {
            if (!_settings.Enabled
                || delta == null
                || string.IsNullOrEmpty(delta.AgentId))
            {
                return false;
            }

            var db = _services.GetRequiredService<IBotSharpRepository>();
            var isSaved = db.SaveGlobalStats(delta);
            return isSaved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when updating global stats {@event} (agent id: {delta?.AgentId}).");
            return false;
        }
    }
}
