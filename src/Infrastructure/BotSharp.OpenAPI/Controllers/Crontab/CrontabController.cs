using BotSharp.Abstraction.Crontab;

namespace BotSharp.OpenAPI.Controllers;

[Authorize]
[ApiController]
public class CrontabController : ControllerBase
{
    private readonly IServiceProvider _services;
    private readonly ILogger<CrontabController> _logger;

    public CrontabController(
        IServiceProvider services,
        ILogger<CrontabController> logger)
    {
        _services = services;
        _logger = logger;
    }

    [HttpPost("/crontab/{name}")]
    public async Task<bool> RunCrontab(string name)
    {
        var cron = _services.GetRequiredService<ICrontabService>();
        var crons = await cron.GetCrontable();
        var found = crons.FirstOrDefault(x => x.Title.IsEqualTo(name));
        if (found == null)
        {
            _logger.LogWarning($"Cannnot find crontab {name}");
            return false;
        }

        try
        {
            _logger.LogWarning($"Start running crontab {name}");
            await cron.ScheduledTimeArrived(found);
            _logger.LogWarning($"Complete running crontab {name}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when running crontab {name}");
            return false;
        }
    }
}
