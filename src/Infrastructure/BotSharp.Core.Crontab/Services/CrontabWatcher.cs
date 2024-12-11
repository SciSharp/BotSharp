using BotSharp.Abstraction.Infrastructures;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NCrontab;

namespace BotSharp.Core.Crontab.Services;

public class CrontabWatcher : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;

    public CrontabWatcher(IServiceProvider services, ILogger<CrontabWatcher> logger)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crontab Watcher background service is running.");

        using (var scope = _services.CreateScope())
        {
            var locker = scope.ServiceProvider.GetRequiredService<IDistributedLocker>();

            while (!stoppingToken.IsCancellationRequested)
            {
                var delay = Task.Delay(1000, stoppingToken);

                await locker.LockAsync("CrontabWatcher", async () =>
                {
                    await RunCronChecker(scope.ServiceProvider);
                });

                await delay;
            }

            _logger.LogWarning("Crontab Watcher background service is stopped.");
        }
    }

    private async Task RunCronChecker(IServiceProvider services)
    {
        var cron = services.GetRequiredService<ICrontabService>();
        var crons = await cron.GetCrontable();
        foreach (var item in crons)
        {
            try
            {
                var schedule = CrontabSchedule.Parse(item.Cron, new CrontabSchedule.ParseOptions
                {
                    IncludingSeconds = true // Ensure you account for seconds
                });

                // Get the current time
                var currentTime = DateTime.UtcNow;

                // Get the next occurrence from the schedule
                var nextOccurrence = schedule.GetNextOccurrence(currentTime.AddSeconds(-1));

                // Check if the current time matches the schedule
                bool matches = currentTime >= nextOccurrence && currentTime < nextOccurrence.AddSeconds(1);

                if (matches)
                {
                    _logger.LogDebug($"The current time matches the cron expression {item}");
                    cron.ScheduledTimeArrived(item);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Error when running cron task ({item.ConversationId}, {item.Title}, {item.Cron}): {ex.Message}\r\n{ex.InnerException}");
                continue;
            }
        }
    }
}
