using BotSharp.Abstraction.Infrastructures.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace BotSharp.Core.Crontab.Services;

public class CrontabEventSubscription : BackgroundService
{
    private readonly ILogger _logger;
    private readonly IServiceProvider _services;

    public CrontabEventSubscription(IServiceProvider services, ILogger<CrontabEventSubscription> logger)
    {
        _logger = logger;
        _services = services;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Crontab event subscription background service is running.");

        using (var scope = _services.CreateScope())
        {
            var subscriber = scope.ServiceProvider.GetRequiredService<IEventSubscriber>();
            var cron = scope.ServiceProvider.GetRequiredService<ICrontabService>();
            var crons = await cron.GetCrontable();
            foreach (var item in crons)
            {
                _ = Task.Run(async () =>
                {
                    await subscriber.SubscribeAsync($"Crontab:{item.Title}",
                        "Crontab",
                        port: 0,
                        priorityEnabled: false, async (sender, args) =>
                        {
                            var scope = _services.CreateScope();
                            cron = scope.ServiceProvider.GetRequiredService<ICrontabService>();
                            await cron.ScheduledTimeArrived(item);
                        }, stoppingToken: stoppingToken);
                });
            }
        }


        /*using (var scope = _services.CreateScope())
        {
            var cron = scope.ServiceProvider.GetRequiredService<ICrontabService>();
            var crons = await cron.GetCrontable();



            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken);
            }
        }*/
    }
}
