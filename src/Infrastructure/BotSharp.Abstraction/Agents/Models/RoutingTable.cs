using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Agents.Models;

public class RoutingTable
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }

    [JsonPropertyName("name")]
    public string AgentName { get; set; }

    [JsonPropertyName("required")]
    public List<string> RequiredFields { get; set; }

    public override string ToString()
    {
        return AgentName;
    }
}
