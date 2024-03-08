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
            _logger.LogInformation("Conversation Timeout Service is running.");
            try
            {
                while (true)
                {
                    stoppingToken.ThrowIfCancellationRequested();
                    var delay = Task.Delay(TimeSpan.FromHours(1));
                    try
                    {
                        await CleanIdleConversationsAsync();
                        await CloseIdleConversationsAsync(TimeSpan.FromMinutes(10));
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

        private async Task CloseIdleConversationsAsync(TimeSpan conversationIdleTimeout)
        {
            using var scope = _services.CreateScope();
            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var hooks = scope.ServiceProvider.GetServices<IConversationHook>()
                .OrderBy(x => x.Priority)
                .ToList();
            var moment = DateTime.UtcNow.Add(-conversationIdleTimeout);
            var conversations = (await conversationService.GetLastConversations()).Where(c => c.CreatedTime <= moment);
            foreach (var conversation in conversations)
            {
                try
                {
                    var response = new RoleDialogModel(AgentRole.Assistant, "End the conversation due to timeout.")
                    {
                        StopCompletion = true,
                        FunctionName = "conversation_end"
                    };

                    foreach (var hook in hooks)
                    {
                        await hook.OnConversationEnding(response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error occurred closing conversation #{conversation.Id}.");
                }
            }
        }

        private async Task CleanIdleConversationsAsync()
        {
            using var scope = _services.CreateScope();
            var settings = scope.ServiceProvider.GetRequiredService<ConversationSetting>();
            var cleanSetting = settings.CleanSetting;

            if (cleanSetting == null || !cleanSetting.Enable) return;

            var conversationService = scope.ServiceProvider.GetRequiredService<IConversationService>();
            var conversationIds = await conversationService.GetIdleConversations(cleanSetting.BatchSize, cleanSetting.MessageLimit, cleanSetting.BufferHours);

            if (!conversationIds.IsNullOrEmpty())
            {
                await conversationService.DeleteConversations(conversationIds);
            }
        }
    }
}
