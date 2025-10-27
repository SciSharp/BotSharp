using BotSharp.Abstraction.Repositories.Options;

namespace BotSharp.Abstraction.Agents.Options;

public class AgentCodeScriptUpdateOptions : AgentCodeScriptDbUpdateOptions
{
    [JsonPropertyName("delete_if_not_included")]
    public bool DeleteIfNotIncluded { get; set; }
}
