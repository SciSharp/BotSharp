namespace BotSharp.Abstraction.Users.Models;

public class UserAuthorization
{
    public bool IsAdmin { get; set; }
    public IEnumerable<string> Permissions { get; set; } = [];
    public IEnumerable<UserAgent> AgentActions { get; set; } = [];
}


public static class UserAuthorizationExtension
{
    public static bool IsAgentActionAllowed(this UserAuthorization auth, string agentId, string targetAction)
    {
        if (auth == null || string.IsNullOrEmpty(agentId)) return false;

        if (auth.IsAdmin) return true;

        var found = auth.AgentActions.FirstOrDefault(x => x.AgentId == agentId);
        if (found == null) return false;

        var actions = found.Actions ?? [];
        return actions.Any(x => x == targetAction);
    }

    /// <summary>
    /// Get allowed user actions on the agent. If user is admin, returns null;
    /// </summary>
    /// <param name="auth"></param>
    /// <param name="agentId"></param>
    /// <returns></returns>
    public static IEnumerable<string>? GetAllowedAgentActions(this UserAuthorization auth, string agentId)
    {
        if (auth == null || string.IsNullOrEmpty(agentId))
        {
            return [];
        }

        if (auth.IsAdmin)
        {
            return null;
        }

        var found = auth.AgentActions.FirstOrDefault(x => x.AgentId == agentId);
        return found?.Actions ?? [];
    }
}