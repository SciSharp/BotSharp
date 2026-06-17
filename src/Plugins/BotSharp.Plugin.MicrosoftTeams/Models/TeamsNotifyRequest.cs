namespace BotSharp.Plugin.MicrosoftTeams.Models;

public class TeamsNotifyRequest
{
    /// <summary>
    /// Target user id (AAD object id) the bot has previously interacted with.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// When set, the agent generates the reply from <see cref="Prompt"/>.
    /// Otherwise <see cref="Text"/> is sent verbatim.
    /// </summary>
    public string? AgentId { get; set; }

    public string? Prompt { get; set; }

    public string? Text { get; set; }
}
