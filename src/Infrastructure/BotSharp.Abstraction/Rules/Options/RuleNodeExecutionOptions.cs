namespace BotSharp.Abstraction.Rules.Options;

public class RuleNodeExecutionOptions
{
    public string AgentId { get; set; }
    public string Text { get; set; }
    public IEnumerable<MessageState> States { get; set; } = [];
    public int? MaxGraphRecursion { get; set; }
}
