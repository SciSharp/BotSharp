namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowLoadOptions
{
    public string? Query { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
