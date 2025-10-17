using BotSharp.Abstraction.Repositories.Options;

namespace BotSharp.Abstraction.Agents.Options;

public class AgentCodeScriptUpdateOptions : AgentCodeScriptDbUpdateOptions
{
    public bool DeleteIfNotIncluded { get; set; }
}
