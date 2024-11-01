namespace BotSharp.Abstraction.Users.Models;

public class UserAgentAction
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonIgnore]
    public Agent? Agent { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<string> Actions { get; set; } = [];
}
