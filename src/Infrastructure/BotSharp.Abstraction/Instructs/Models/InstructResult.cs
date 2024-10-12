namespace BotSharp.Abstraction.Instructs.Models;

public class InstructResult : ITrackableMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
    public string Text { get; set; }
    public object? Data { get; set; }
    public Dictionary<string, string>? States { get; set; } = new();
}
