namespace BotSharp.Abstraction.Instructs.Models;

public class InstructResult : ITrackableMessage
{
    public string MessageId { get; set; }
    public string Text { get; set; }
    public object Data { get; set; }
    public ConversationState States { get; set; }
}
