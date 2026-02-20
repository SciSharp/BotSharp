using BotSharp.Abstraction.Repositories.Filters;
using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Options;

public class RuleTriggerOptions
{
    /// <summary>
    /// Filter agents
    /// </summary>
    public AgentFilter? AgentFilter { get; set; }

    /// <summary>
    /// Json serializer options
    /// </summary>
    public JsonSerializerOptions? JsonOptions { get; set; }
}
