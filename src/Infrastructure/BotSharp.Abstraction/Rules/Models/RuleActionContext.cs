using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleActionContext
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = [];
    public IEnumerable<RuleActionStepResult> PrevStepResults { get; set; } = [];
    public JsonSerializerOptions? JsonOptions { get; set; }
}
