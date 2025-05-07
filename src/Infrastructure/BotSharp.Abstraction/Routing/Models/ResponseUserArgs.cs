namespace BotSharp.Abstraction.Routing.Models;

public class ResponseUserArgs
{
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;
}
