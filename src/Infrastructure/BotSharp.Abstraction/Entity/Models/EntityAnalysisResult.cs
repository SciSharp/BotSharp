namespace BotSharp.Abstraction.Entity.Models;

public class EntityAnalysisResult
{
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("canonical_text")]
    public string? CanonicalText { get; set; }

    [JsonPropertyName("data")]
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
