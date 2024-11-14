namespace BotSharp.Abstraction.Roles.Models;

public class RoleAgent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string RoleId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<string> Actions { get; set; } = [];

    [JsonIgnore]
    public Agent? Agent { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }
}
