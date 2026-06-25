using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.OpenAPI.RuleTriggers;
using Microsoft.Extensions.Hosting;

namespace BotSharp.OpenAPI.Hooks
{
    public class InstructionLogCleanupCrontabHook : ICrontabHook
    {
        private readonly ConversationSetting _settings;
        private readonly IBotSharpRepository _db;
        private readonly ILogger<InstructionLogCleanupCrontabHook> _logger;
        private readonly IHostApplicationLifetime _appLifetime;

        public string[]? Triggers => new[] { nameof(InstructionLogCleanupRuleTrigger) };

        public InstructionLogCleanupCrontabHook(
            ConversationSetting settings,
            IBotSharpRepository db,
            ILogger<InstructionLogCleanupCrontabHook> logger,
            IHostApplicationLifetime appLifetime)
        {
            _settings = settings;
            _db = db;
            _logger = logger;
            _appLifetime = appLifetime;
        }

        public async Task OnCronTriggered(CrontabItem item)
        {
            var cleanSetting = _settings.CleanSetting;

            if (cleanSetting == null || !cleanSetting.Enable || cleanSetting.LogRetentionDays <= 0) return;

            int totalDeleted = 0;
            var cancellationToken = _appLifetime.ApplicationStopping;

            while (!cancellationToken.IsCancellationRequested)
            {
                var deletedCount = await _db.DeleteOldInstructionLogs(cleanSetting.LogRetentionDays, cleanSetting.LogBatchSize);
                if (deletedCount == 0) break;

                totalDeleted += deletedCount;
                _logger.LogInformation($"Cleaned {deletedCount} instruction logs older than {cleanSetting.LogRetentionDays} days in this batch.");

                try
                {
                    // Sleep slightly to yield database resources, will throw TaskCanceledException on shutdown
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Instruction log cleanup was interrupted due to application shutdown.");
                    break;
                }
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation($"Successfully cleaned a total of {totalDeleted} instruction logs older than {cleanSetting.LogRetentionDays} days.");
            }
        }
    }
}
