namespace BotSharp.Abstraction.Rules.Models;

public class RuleActionContext
{
    public string Text { get; set; }
    public IEnumerable<MessageState>? States { get; set; }
}
