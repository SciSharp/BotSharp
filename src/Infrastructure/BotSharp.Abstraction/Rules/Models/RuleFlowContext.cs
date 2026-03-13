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
    public RuleNode Node { get; set; }
    public RuleEdge Edge { get; set; }
    public RuleGraph Graph { get; set; }
}

