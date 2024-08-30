namespace BotSharp.Abstraction.Agents.Enums;

public class AgentType
{
    /// <summary>
    /// Routing agent
    /// </summary>
    public const string Routing = "routing";

    /// <summary>
    /// Planning agent
    /// </summary>
    public const string Planning = "plan";

    public const string Evaluating = "evaluation";

    /// <summary>
    /// Routable task agent with capability of interaction with external environment
    /// </summary>
    public const string Task = "task";

    /// <summary>
    /// Agent that cannot use external tools
    /// </summary>
    public const string Static = "static";
}

