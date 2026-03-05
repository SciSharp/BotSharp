namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowOptions
{
    /// <summary>
    /// Config provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Config id
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Additional custom parameters, e.g., root_node_name, max_recursion
    /// </summary>
    public Dictionary<string, string> Parameters { get; set; } = [];
}
