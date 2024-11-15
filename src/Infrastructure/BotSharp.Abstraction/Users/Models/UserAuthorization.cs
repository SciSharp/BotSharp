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
}