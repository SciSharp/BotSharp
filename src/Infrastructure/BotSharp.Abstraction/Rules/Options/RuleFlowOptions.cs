namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowOptions
{
    /// <summary>
    /// Flow provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Flow topology id
    /// </summary>
    public string TopologyId { get; set; } = string.Empty;

    /// <summary>
    /// Additional custom parameters, e.g., root_node_name, max_recursion
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = [];
}
