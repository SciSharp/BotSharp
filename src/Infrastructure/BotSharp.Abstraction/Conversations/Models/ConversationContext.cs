namespace BotSharp.Abstraction.Conversations.Models;

public class ConversationContext
{
    public ConversationState State { get; set; }
    public List<DialogElement> Dialogs { get; set; } = new();
    public List<ConversationBreakpoint> Breakpoints { get; set; } = new();
    public int RecursiveCounter { get; set; }
    public Stack<string> RoutingStack { get; set; } = new();
}
