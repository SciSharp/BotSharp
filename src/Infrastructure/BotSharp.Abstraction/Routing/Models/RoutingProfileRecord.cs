using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingProfileRecord
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("agent_ids")]
    public string[] AgentIds { get; set; }
}
