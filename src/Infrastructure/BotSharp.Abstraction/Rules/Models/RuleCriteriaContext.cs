namespace BotSharp.Abstraction.Rules.Models;

public class RuleCriteriaContext
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, object?> Parameters { get; set; } = [];
}
