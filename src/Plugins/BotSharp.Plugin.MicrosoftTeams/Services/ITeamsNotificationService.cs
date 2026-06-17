namespace BotSharp.Plugin.MicrosoftTeams.Services;

/// <summary>
/// Outbound (proactive) messaging API — pushes messages to a Teams user the bot has
/// previously interacted with, without the user prompting first.
/// </summary>
public interface ITeamsNotificationService
{
    /// <summary>
    /// Push a literal text message to the user.
    /// </summary>
    /// <returns>false when no conversation reference is known for the user.</returns>
    Task<bool> SendTextAsync(string userId, string text, CancellationToken cancellationToken = default);

    /// <summary>
    /// Run the given agent with <paramref name="prompt"/> and push its reply to the user.
    /// </summary>
    Task<bool> NotifyAsync(string userId, string agentId, string prompt, CancellationToken cancellationToken = default);
}
