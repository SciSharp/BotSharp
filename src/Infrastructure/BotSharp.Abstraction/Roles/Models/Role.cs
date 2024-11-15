namespace BotSharp.Abstraction.Roles.Models;

public class Role
{
    [JsonPropertyName("id")]
    public string Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("permissions")]
    public IEnumerable<string> Permissions { get; set; } = [];

    [JsonIgnore]
    public IEnumerable<RoleAgentAction> AgentActions { get; set; } = [];

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; }

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; }
}
