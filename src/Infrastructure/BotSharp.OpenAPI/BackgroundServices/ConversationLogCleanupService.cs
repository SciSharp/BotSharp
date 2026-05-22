using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace BotSharp.OpenAPI.BackgroundServices
{
    public class ConversationLogCleanupService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ConversationLogCleanupService> _logger;

        public ConversationLogCleanupService(IServiceProvider services, ILogger<ConversationLogCleanupService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Log Cleanup Service is running...");

            _ = Task.Run(async () =>
            {
                await DoWork(stoppingToken);
            });
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Log Cleanup Service is doing work...");

            try
            {
                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    // Run once a day
                    var delay = Task.Delay(TimeSpan.FromHours(24), stoppingToken);
                    try
                    {
                        await CleanOldLogsAsync(stoppingToken);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error occurred cleaning old conversation logs.");
                    }
                    await delay;
                }
            }
            catch (OperationCanceledException) { }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Log Cleanup Service is stopping.");
            await base.StopAsync(stoppingToken);
        }

        private async Task CleanOldLogsAsync(CancellationToken stoppingToken)
        {
            using var scope = _services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ConversationSetting>();
            var cleanSetting = settings.CleanSetting;

            if (cleanSetting == null || !cleanSetting.Enable || cleanSetting.LogRetentionDays <= 0) return;

            var locker = scope.ServiceProvider.GetRequiredService<BotSharp.Abstraction.Infrastructures.IDistributedLocker>();
            var lockResource = nameof(ConversationLogCleanupService);
            await locker.LockAsync(lockResource, async () =>
            {
                await ExecuteCleanup(scope.ServiceProvider, cleanSetting.LogRetentionDays, cleanSetting.LogBatchSize, stoppingToken);
            }, timeout: 3600); // 1 hour lock timeout to prevent other pods from running it
        }

        private async Task ExecuteCleanup(IServiceProvider serviceProvider, int retentionDays, int batchSize, CancellationToken stoppingToken)
        {
            var db = serviceProvider.GetRequiredService<IBotSharpRepository>();
            int totalDeleted = 0;
            
            while (!stoppingToken.IsCancellationRequested)
            {
                var deletedCount = await db.DeleteOldConversationLogs(retentionDays, batchSize);
                if (deletedCount == 0) break;
                
                totalDeleted += deletedCount;
                _logger.LogInformation($"Cleaned {deletedCount} conversation logs older than {retentionDays} days in this batch.");
                
                // Sleep slightly to yield database resources
                await Task.Delay(1000, stoppingToken);
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation($"Successfully cleaned a total of {totalDeleted} conversation logs older than {retentionDays} days.");
            }
        }
    }
}
