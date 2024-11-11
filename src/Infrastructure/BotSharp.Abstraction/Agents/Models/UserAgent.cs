using BotSharp.Abstraction.Users.Models;

namespace BotSharp.Abstraction.Agents.Models;

public class UserAgent
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("user_id")]
    public string UserId { get; set; } = string.Empty;

    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("actions")]
    public IEnumerable<string> Actions { get; set; } = [];

    [JsonIgnore]
    public Agent? Agent { get; set; }

    [JsonPropertyName("updated_time")]
    public DateTime UpdatedTime { get; set; } = DateTime.UtcNow;

    [JsonPropertyName("created_time")]
    public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
}
