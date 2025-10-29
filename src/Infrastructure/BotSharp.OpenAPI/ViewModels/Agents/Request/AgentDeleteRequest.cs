using BotSharp.Abstraction.Agents.Options;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentDeleteRequest
{
    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentDeleteOptions? Options { get; set; }
}
