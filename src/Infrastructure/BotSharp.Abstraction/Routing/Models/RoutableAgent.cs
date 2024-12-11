using BotSharp.Abstraction.Functions.Models;

namespace BotSharp.Abstraction.Routing.Models;

public class RoutableAgent
{
    [JsonPropertyName("agent_id")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = AgentType.Task;

    [JsonPropertyName("profiles")]
    public List<string> Profiles { get; set; }
        = new List<string>();

    [JsonPropertyName("required_fields")]
    public List<ParameterPropertyDef> RequiredFields { get; set; } = new List<ParameterPropertyDef>();

    [JsonPropertyName("optional_fields")]
    public List<ParameterPropertyDef> OptionalFields { get; set; } = new List<ParameterPropertyDef>();

    public override string ToString()
    {
        return Name;
    }
}
