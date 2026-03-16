namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowOptions
{
    /// <summary>
    /// Flow topology provider
    /// </summary>
    [JsonPropertyName("topology_provider")]
    public string? TopologyProvider { get; set; }

    /// <summary>
    /// Flow topology name
    /// </summary>
    [JsonPropertyName("topology_name")]
    public string? TopologyName { get; set; }

    /// <summary>
    /// Query to get flow topology
    /// </summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>
    /// Graph traversal algorithm: "dfs" (default) or "bfs"
    /// </summary>
    [JsonPropertyName("traversal_algorithm")]
    public string TraversalAlgorithm { get; set; } = "dfs";

    /// <summary>
    /// Additional custom parameters, e.g., root_node_name, max_recursion
    /// </summary>
    [JsonPropertyName("parameters")]
    public Dictionary<string, object> Parameters { get; set; } = [];
}
