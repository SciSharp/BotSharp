namespace BotSharp.Abstraction.Routing.Models;

public class FallbackArgs
{
    [JsonPropertyName("fallback_reason")]
    public string Reason { get; set; } = null!;

    [JsonPropertyName("user_question")]
    public string Question { get; set; } = null;
}
