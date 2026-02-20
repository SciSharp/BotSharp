namespace BotSharp.Abstraction.Rules.Options;

public class RuleExecutionActionOptions
{
    public string AgentId { get; set; }
    public string Text { get; set; }
    public IEnumerable<MessageState> States { get; set; } = [];
}
