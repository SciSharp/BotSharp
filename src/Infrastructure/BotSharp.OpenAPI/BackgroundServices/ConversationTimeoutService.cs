using Microsoft.Extensions.Hosting;

namespace BotSharp.OpenAPI.BackgroundServices
{
    public class ConversationTimeoutService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<ConversationTimeoutService> _logger;

        public ConversationTimeoutService(IServiceProvider services, ILogger<ConversationTimeoutService> logger)
        {
            _services = services;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Timeout Service is running...");

            _ = Task.Run(async () =>
            {
                await DoWork(stoppingToken);
            });
        }

        private async Task DoWork(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Timeout Service is doing work...");

            try
            {
                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    var delay = Task.Delay(TimeSpan.FromHours(1));
                    try
                    {
                        await CleanIdleConversationsAsync();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error occurred closing conversations.");
                    }
                    await delay;
                }
            }
            catch (OperationCanceledException) { }
        }

        public override async Task StopAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Conversation Timeout Service is stopping.");
            await base.StopAsync(stoppingToken);
        }

        private async Task CleanIdleConversationsAsync()
        {
            using var scope = _services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ConversationSetting>();
            var cleanSetting = settings.CleanSetting;

            if (cleanSetting == null || !cleanSetting.Enable) return;

            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var conversationIds = await conversationService.GetIdleConversations(cleanSetting.BatchSize, cleanSetting.MessageLimit, cleanSetting.BufferHours, cleanSetting.ExcludeAgentIds);

            if (!conversationIds.IsNullOrEmpty())
            {
                await conversationService.DeleteConversations(conversationIds);
            }
        }
    }
}
