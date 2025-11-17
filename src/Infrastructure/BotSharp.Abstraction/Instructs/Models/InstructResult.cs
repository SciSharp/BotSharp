namespace BotSharp.Abstraction.Instructs.Models;

public class InstructResult : ITrackableMessage
{
    [JsonPropertyName("message_id")]
    public string MessageId { get; set; }
    public string Text { get; set; } = string.Empty;
    public object? Data { get; set; }

    [JsonPropertyName("template")]
    public string? Template { get; set; }
    public Dictionary<string, string>? States { get; set; } = new();

    [JsonPropertyName("log_id")]
    public string LogId { get; set; } = default!;
}
