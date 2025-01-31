using BotSharp.Abstraction.Infrastructures;
using BotSharp.Abstraction.Infrastructures.Events;
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

                await locker.LockAsync("CrontabWatcher:locker", async () =>
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
        var publisher = services.GetService<IEventPublisher>();

        foreach (var item in crons)
        {
            _logger.LogDebug($"[{DateTime.UtcNow}] Cron task ({item.Title}, {item.Cron}), Last Execution Time:  {item.LastExecutionTime}");

            try
            {
                // strip seconds from cron expression
                item.Cron = string.Join(" ", item.Cron.Split(' ').TakeLast(5));
                var schedule = CrontabSchedule.Parse(item.Cron, new CrontabSchedule.ParseOptions
                {
                    IncludingSeconds = false // Ensure you account for seconds
                });

                // Get the current time
                var currentTime = DateTime.UtcNow;

                // Get the last occurrence from the schedule
                var lastOccurrence = GetLastOccurrence(schedule);

                // Get the next occurrence from the schedule
                var nextOccurrence = schedule.GetNextOccurrence(currentTime.AddSeconds(-1));

                // Get the previous occurrence from the execution log
                var previousOccurrence = item.LastExecutionTime;

                // First check if this occurrence was already triggered
                if (previousOccurrence.HasValue && 
                    previousOccurrence.Value >= lastOccurrence && 
                    previousOccurrence.Value < nextOccurrence.AddSeconds(1))
                {
                    continue;
                }

                // Then check if the current time matches the schedule
                bool matches = currentTime >= nextOccurrence && currentTime < nextOccurrence.AddSeconds(1);

                if (matches)
                {
                    _logger.LogInformation($"The current time matches the cron expression {item}");

                    if (publisher != null)
                    {
                        await publisher.PublishAsync($"Crontab:{item.Title}", item.Cron);
                    }
                    else
                    {
                        var scope = _services.CreateScope();
                        cron = scope.ServiceProvider.GetRequiredService<ICrontabService>();
                        cron.ScheduledTimeArrived(item);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error when running cron task ({item.Title}, {item.Cron}): {ex.Message}");
                continue;
            }
        }
    }

    private DateTime GetLastOccurrence(CrontabSchedule schedule)
    {
        var nextOccurrence = schedule.GetNextOccurrence(DateTime.UtcNow);
        var afterNextOccurrence = schedule.GetNextOccurrence(nextOccurrence);
        var interval = afterNextOccurrence - nextOccurrence;
        if (interval.TotalMinutes < 1)
        {
            throw new ArgumentException("The minimum interval must be at least 1 minutes.");
        }
        return nextOccurrence - interval;
    }
}
