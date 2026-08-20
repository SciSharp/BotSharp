using Microsoft.Agents.Authentication;
using Microsoft.Agents.Builder;
using Microsoft.Agents.Core.Models;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

public class TeamsNotificationService : ITeamsNotificationService
{
    private readonly IChannelAdapter _adapter;
    private readonly IConversationReferenceStore _referenceStore;
    private readonly AdaptiveCardConverter _cardConverter;
    private readonly MicrosoftTeamsSetting _setting;
    private readonly IServiceProvider _services;
    private readonly ILogger<TeamsNotificationService> _logger;

    public TeamsNotificationService(
        IChannelAdapter adapter,
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

        var identity = AgentClaims.CreateIdentity(_setting.AppId, true, null);
        await _adapter.ContinueConversationAsync(identity, reference,
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

        var identity = AgentClaims.CreateIdentity(_setting.AppId, true, null);
        await _adapter.ContinueConversationAsync(identity, reference,
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
