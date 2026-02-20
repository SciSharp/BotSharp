using System.Text.Json;

namespace BotSharp.Abstraction.Rules.Models;

public class RuleCriteriaContext
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, string?> Parameters { get; set; } = [];
    public JsonSerializerOptions? JsonOptions { get; set; }
}
