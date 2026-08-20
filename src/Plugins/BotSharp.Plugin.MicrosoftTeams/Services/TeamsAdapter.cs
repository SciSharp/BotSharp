using Microsoft.Agents.Builder;
using Microsoft.Agents.Hosting.AspNetCore;
using Microsoft.Agents.Hosting.AspNetCore.BackgroundQueue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// CloudAdapter with a turn-level error handler. Used for both inbound processing
/// (<c>ProcessAsync</c>) and proactive delivery (<c>ContinueConversationAsync</c>).
/// </summary>
public class TeamsAdapter : CloudAdapter
{
    public TeamsAdapter(
        IChannelServiceClientFactory channelServiceClientFactory,
        IActivityTaskQueue activityTaskQueue,
        ILogger<TeamsAdapter> logger,
        IConfiguration configuration)
        : base(channelServiceClientFactory, activityTaskQueue, logger, null, null, configuration)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "Teams turn error: {Message}", exception.Message);
            await turnContext.SendActivityAsync("Sorry, something went wrong handling your message.");
        };
    }
}
