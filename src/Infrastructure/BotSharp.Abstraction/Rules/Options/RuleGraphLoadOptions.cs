namespace BotSharp.Abstraction.Rules.Options;

public class RuleGraphLoadOptions
{
    public string? AgentId { get; set; }
    public string? Trigger { get; set; }
    public IEnumerable<MessageState>? States { get; set; }
}
