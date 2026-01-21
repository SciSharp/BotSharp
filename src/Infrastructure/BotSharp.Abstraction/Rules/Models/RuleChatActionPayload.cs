namespace BotSharp.Abstraction.Rules.Models;

public class RuleChatActionPayload
{
    public string Text { get; set; }
    public string Channel { get; set; }
    public IEnumerable<MessageState>? States { get; set; }
}
