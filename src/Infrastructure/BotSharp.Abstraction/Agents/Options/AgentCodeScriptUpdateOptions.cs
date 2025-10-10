using BotSharp.Abstraction.Repositories.Models;

namespace BotSharp.Abstraction.Agents.Options;

public class AgentCodeScriptUpdateOptions : AgentCodeScriptDbUpdateOptions
{
    public bool DeleteIfNotIncluded { get; set; }
}
