namespace BotSharp.Abstraction.Tokenizers.Models;

public class TokenizeResult
{
    public string Token { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public IDictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}
