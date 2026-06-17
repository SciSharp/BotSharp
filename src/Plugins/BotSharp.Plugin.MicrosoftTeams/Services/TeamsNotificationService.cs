using Microsoft.Bot.Builder;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

public class TeamsNotificationService : ITeamsNotificationService
{
    private readonly TeamsAdapter _adapter;
    private readonly IConversationReferenceStore _referenceStore;
    private readonly AdaptiveCardConverter _cardConverter;
    private readonly MicrosoftTeamsSetting _setting;
    private readonly IServiceProvider _services;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(
        TeamsAdapter adapter,
        IConversationReferenceStore referenceStore,
        AdaptiveCardConverter cardConverter,
        MicrosoftTeamsSetting setting,
        IServiceProvider services,
        ILogger<TeamsNotificationService> logger)
    {
        _adapter = adapter;
        _referenceStore = referenceStore;
        _cardConverter = cardConverter;
        _setting = setting;
        _services = services;
        _logger = logger;
    }

    public async Task<bool> SendTextAsync(string userId, string text, CancellationToken cancellationToken = default)
    {
        var reference = await _referenceStore.GetAsync(userId);
        if (reference == null)
        {
            _logger.LogWarning("Teams: no conversation reference for user {UserId}; cannot push message.", userId);
            return false;
        }

        await _adapter.ContinueConversationAsync(_setting.AppId, reference,
            async (turnContext, ct) => await turnContext.SendActivityAsync(MessageFactory.Text(text), ct),
            cancellationToken);
        return true;
    }

    public async Task<bool> NotifyAsync(string userId, string agentId, string prompt, CancellationToken cancellationToken = default)
    {
        var reference = await _referenceStore.GetAsync(userId);
        if (reference == null)
        {
            _logger.LogWarning("Teams: no conversation reference for user {UserId}; cannot notify.", userId);
            return false;
        }

        await _adapter.ContinueConversationAsync(_setting.AppId, reference,
            async (turnContext, ct) =>
            {
                // Proactive turns run outside the request scope — create a fresh DI scope.
                using var scope = _services.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<TeamsMessageHandler>();
                await handler.Handle(userId, agentId, prompt,
                    activity => turnContext.SendActivityAsync(activity, ct));
            },
            cancellationToken);
        return true;
    }
}
