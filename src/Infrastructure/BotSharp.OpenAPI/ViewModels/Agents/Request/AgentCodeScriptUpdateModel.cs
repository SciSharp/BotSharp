using BotSharp.Abstraction.Agents.Models;
using BotSharp.Abstraction.Agents.Options;
using System.Text.Json.Serialization;

namespace BotSharp.OpenAPI.ViewModels.Agents;

public class AgentCodeScriptUpdateModel
{
    [JsonPropertyName("code_scripts")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<AgentCodeScriptViewModel>? CodeScripts { get; set; }

    [JsonPropertyName("options")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public AgentCodeScriptUpdateOptions? Options { get; set; }
}
