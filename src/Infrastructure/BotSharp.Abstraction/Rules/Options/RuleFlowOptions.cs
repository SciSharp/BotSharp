namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowOptions
{
    /// <summary>
    /// Flow topology provider
    /// </summary>
    public string? TopologyProvider { get; set; }

    /// <summary>
    /// Flow topology id
    /// </summary>
    public string? TopologyId { get; set; }

    /// <summary>
    /// Query to get flow topology
    /// </summary>
    public string? Query { get; set; }

    /// <summary>
    /// Additional custom parameters, e.g., root_node_name, max_recursion
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = [];
}
