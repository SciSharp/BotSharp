using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Agents.Models;

public class AgentRoutingArgs
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; }
}
