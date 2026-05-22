using BotSharp.Abstraction.Conversations.Settings;
using BotSharp.Abstraction.Crontab;
using BotSharp.Abstraction.Crontab.Models;
using BotSharp.Abstraction.Repositories;
using BotSharp.OpenAPI.RuleTriggers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BotSharp.OpenAPI.Hooks
{
    public class ConversationLogCleanupCrontabHook : ICrontabHook
    {
        private readonly ConversationSetting _settings;
        private readonly IBotSharpRepository _db;
        private readonly ILogger<ConversationLogCleanupCrontabHook> _logger;
        private readonly IHostApplicationLifetime _appLifetime;
        
        public string[]? Triggers => new[] { nameof(ConversationLogCleanupRuleTrigger) };

        public ConversationLogCleanupCrontabHook(
            ConversationSetting settings,
            IBotSharpRepository db,
            ILogger<ConversationLogCleanupCrontabHook> logger,
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
                var deletedCount = await _db.DeleteOldConversationLogs(cleanSetting.LogRetentionDays, cleanSetting.LogBatchSize);
                if (deletedCount == 0) break;
                
                totalDeleted += deletedCount;
                _logger.LogInformation($"Cleaned {deletedCount} conversation logs older than {cleanSetting.LogRetentionDays} days in this batch.");
                
                try
                {
                    // Sleep slightly to yield database resources, will throw TaskCanceledException on shutdown
                    await Task.Delay(1000, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    _logger.LogWarning("Conversation log cleanup was interrupted due to application shutdown.");
                    break;
                }
            }

            if (totalDeleted > 0)
            {
                _logger.LogInformation($"Successfully cleaned a total of {totalDeleted} conversation logs older than {cleanSetting.LogRetentionDays} days.");
            }
        }
    }
}
