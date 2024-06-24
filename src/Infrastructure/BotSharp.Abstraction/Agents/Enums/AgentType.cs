namespace BotSharp.Abstraction.Agents.Enums;

public class AgentType
{
    /// <summary>
    /// Routing Agent
    /// </summary>
    public const string Routing = "routing";

    public const string Evaluating = "evaluating";

    /// <summary>
    /// Routable task agent with capability of interaction with external environment
    /// </summary>
    public const string Task = "task";

    /// <summary>
    /// Agent that cannot use external tools
    /// </summary>
    public const string Static = "static";

    public const string Tool = "tool";
}

