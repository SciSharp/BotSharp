using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Core.Crontab.Services;

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

    /// <summary>
    /// As the Dkron job trigger API, run every 1 minutes
    /// </summary>
    /// <returns></returns>
    [HttpPost("/crontab/scheduling-per-minute")]
    public async Task SchedulingCrontab()
    {
        var allowedCrons = await GetCrontabItems();

        foreach (var item in allowedCrons)
        {
            if (item.CheckNextOccurrenceEveryOneMinute())
            {
                _logger.LogInformation("Crontab: {0}, One occurrence was matched, Beginning execution...", item.Title);
                Task.Run(() => ExecuteTimeArrivedItem(item));
            }
        }
    }

    private async Task<List<CrontabItem>> GetCrontabItems(string? title = null)
    {
        var crontabService = _services.GetRequiredService<ICrontabService>();
        var crons = await crontabService.GetCrontable();
        var allowedCrons = crons.Where(cron => cron.TriggerByOpenAPI).ToList();

        if (title is null)
        {
            return allowedCrons;
        }

        return allowedCrons.Where(cron => cron.Title.IsEqualTo(title)).ToList();
    }

    private async Task<bool> ExecuteTimeArrivedItem(CrontabItem item)
    {
        try
        {
            using var scope = _services.CreateScope();
            var crontabService = scope.ServiceProvider.GetRequiredService<ICrontabService>();
            _logger.LogWarning($"Start running crontab {item.Title}");
            await crontabService.ScheduledTimeArrived(item);
            _logger.LogWarning($"Complete running crontab {item.Title}");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error when running crontab {item.Title}");
            return false;
        }
    }
}
