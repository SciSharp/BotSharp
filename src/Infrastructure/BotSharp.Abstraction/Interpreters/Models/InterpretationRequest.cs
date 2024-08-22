namespace BotSharp.Abstraction.Interpreters.Models;

public class InterpretationRequest
{
    [JsonPropertyName("script")]
    public string Script { get; set; } = null!;

    [JsonPropertyName("language")]
    public string Language { get; set; } = null!;
}
