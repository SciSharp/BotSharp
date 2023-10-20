using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutingItem
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("required_fields")]
    public List<ParameterPropertyDef> RequiredFields { get; set; } = new List<ParameterPropertyDef>();
}
