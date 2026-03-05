namespace BotSharp.Abstraction.Rules.Options;

public class RuleFlowLoadOptions
{
    public string? AgentId { get; set; }
    public string? Trigger { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
}
