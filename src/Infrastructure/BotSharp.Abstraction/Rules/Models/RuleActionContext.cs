namespace BotSharp.Abstraction.Rules.Models;

public class RuleActionContext
{
    public string Text { get; set; } = string.Empty;
    public Dictionary<string, object?> States { get; set; } = [];
}
