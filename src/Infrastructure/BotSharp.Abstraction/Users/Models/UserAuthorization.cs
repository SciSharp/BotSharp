namespace BotSharp.Abstraction.Users.Models;

public class UserAuthorization
{
    public bool IsAdmin { get; set; }
    public IEnumerable<string> Permissions { get; set; } = [];
    public IEnumerable<string> AgentActions { get; set; } = [];
}
