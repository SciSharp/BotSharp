namespace BotSharp.Plugin.MicrosoftTeams.Models;

/// <summary>
/// Per-request scoped holder that carries the routed agentId from the controller
/// into the <c>IBot</c> turn (both are resolved within the same HTTP request scope).
/// </summary>
public class TeamsRequestState
{
    public string AgentId { get; set; } = string.Empty;
}
