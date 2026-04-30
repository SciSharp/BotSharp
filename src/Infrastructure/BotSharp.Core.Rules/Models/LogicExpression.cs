using System.Text.Json.Serialization;

namespace BotSharp.Core.Rules.Models;

/// <summary>
/// Represents a node in a logic expression tree used by LogicGateCondition.
///
/// Leaf node: references a parent node's result by alias and an optional custom data key.
///   e.g. { "node_alias": "check_work_order", "key": "work_order_valid" }
///
/// Operator node: combines children with "and", "or", or "not".
///   e.g. { "op": "and", "children": [ ... ] }
/// </summary>
public class LogicExpression
{
    /// <summary>
    /// For leaf nodes: the parent node alias whose result to inspect.
    /// Using Alias avoids collisions when multiple nodes share the same Name.
    /// </summary>
    [JsonPropertyName("node_alias")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? NodeAlias { get; set; }

    /// <summary>
    /// For leaf nodes: the key in the parent node's Data dictionary that holds the boolean value.
    /// If null or empty, falls back to the parent node's Success flag.
    /// </summary>
    [JsonPropertyName("key")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Key { get; set; }

    /// <summary>
    /// For operator nodes: "and", "or", or "not".
    /// </summary>
    [JsonPropertyName("op")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? Op { get; set; }

    /// <summary>
    /// For operator nodes: the child expressions to combine.
    /// </summary>
    [JsonPropertyName("children")]
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<LogicExpression>? Children { get; set; }
}
