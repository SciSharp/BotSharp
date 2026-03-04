namespace BotSharp.Abstraction.Rules.Options;

public class RuleGraphOptions
{
    /// <summary>
    /// Graph provider
    /// </summary>
    public string Provider { get; set; } = string.Empty;

    /// <summary>
    /// Graph id
    /// </summary>
    public string GraphId { get; set; } = string.Empty;

    /// <summary>
    /// The name of the root node
    /// </summary>
    public string? RootNodeName { get; set; }

    /// <summary>
    /// Max number of action node execution (prevent endless loop)
    /// </summary>
    public int? MaxGraphRecursion { get; set; }
}
