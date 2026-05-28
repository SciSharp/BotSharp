using BotSharp.Abstraction.Graph;
using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Models;

/// <summary>
/// Context for rule flow execution (actions and conditions)
/// </summary>
public class RuleFlowContext
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; set; } = [];
    public IEnumerable<RuleFlowStepResult> PrevStepResults { get; set; } = [];
    public JsonSerializerOptions? JsonOptions { get; set; }
    public FlowNode Node { get; set; }
    public FlowEdge Edge { get; set; }
    public FlowGraph Graph { get; set; }
}

