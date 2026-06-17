using Microsoft.Bot.Schema;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Bridges an inbound Teams message into the BotSharp conversation engine and streams the
/// agent's replies back through the supplied send delegate.
/// </summary>
public class TeamsMessageHandler
{
    private readonly IServiceProvider _services;
    private readonly AdaptiveCardConverter _cardConverter;
    private readonly ILogger<TeamsMessageHandler> _logger;

    public TeamsMessageHandler(
        IServiceProvider services,
        AdaptiveCardConverter cardConverter,
        ILogger<TeamsMessageHandler> logger)
    {
        _services = services;
        _cardConverter = cardConverter;
        _logger = logger;
    }

    /// <param name="userId">Stable per-user id used as the BotSharp conversation id (AAD object id when available).</param>
    /// <param name="agentId">Target BotSharp agent.</param>
    /// <param name="message">User utterance (mention already stripped).</param>
    /// <param name="sendActivity">Callback that delivers an activity back to Teams.</param>
    public async Task Handle(string userId, string agentId, string message, Func<IActivity, Task> sendActivity)
    {
        var inputMsg = new RoleDialogModel(AgentRole.User, message);
        var conv = _services.GetRequiredService<IConversationService>();
        var routing = _services.GetRequiredService<IRoutingService>();

        routing.Context.SetMessageId(userId, inputMsg.MessageId);
        await conv.SetConversationId(userId, new List<MessageState>
        {
            new MessageState("channel", ConversationChannel.Teams)
        });

        var replies = new List<IActivity>();
        await conv.SendMessage(agentId,
            inputMsg,
            replyMessage: null,
            async msg =>
            {
                replies.Add(_cardConverter.Convert(msg));
                await Task.CompletedTask;
            });

        foreach (var reply in replies)
        {
            await sendActivity(reply);
        }

        _logger.LogInformation("Teams: handled message from {UserId} on agent {AgentId}, {Count} reply(ies).",
            userId, agentId, replies.Count);
    }
}
