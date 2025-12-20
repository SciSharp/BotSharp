namespace BotSharp.Abstraction.Conversations.Models;

public class UpdateMessageRequest
{
    public DialogElement Message { get; set; } = null!;
    public int InnerIndex { get; set; }
}
