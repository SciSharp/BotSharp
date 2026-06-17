using Microsoft.Bot.Builder.Integration.AspNet.Core;
using Microsoft.Bot.Connector.Authentication;

namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// CloudAdapter with a turn-level error handler. Used for both inbound processing
/// (<c>ProcessAsync</c>) and proactive delivery (<c>ContinueConversationAsync</c>).
/// </summary>
public class TeamsAdapter : CloudAdapter
{
    public TeamsAdapter(
        BotFrameworkAuthentication auth,
        ILogger<TeamsAdapter> logger)
        : base(auth, logger)
    {
        OnTurnError = async (turnContext, exception) =>
        {
            logger.LogError(exception, "Teams turn error: {Message}", exception.Message);
            await turnContext.SendActivityAsync("Sorry, something went wrong handling your message.");
        };
    }
}
