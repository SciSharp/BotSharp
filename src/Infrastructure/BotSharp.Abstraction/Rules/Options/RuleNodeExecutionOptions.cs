namespace BotSharp.Abstraction.Rules.Options;

public class RuleNodeExecutionOptions
{
    public string Text { get; set; }
    public IEnumerable<MessageState> States { get; set; } = [];
    public RuleGraphOptions? GraphOptions { get; set; }
}
