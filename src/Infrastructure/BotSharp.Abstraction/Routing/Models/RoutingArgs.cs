using System.Text.Json.Serialization;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingArgs
{
    [JsonPropertyName("agent_name")]
    public string AgentName { get; set; }

    public override string ToString()
    {
        return AgentName;
    }
}
