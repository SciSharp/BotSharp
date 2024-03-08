namespace BotSharp.Abstraction.Browsing.Models;

public class ElementLocatingArgs
{
    [JsonPropertyName("match_rule")]
    public string MatchRule { get; set; } = string.Empty;

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("attribute_name")]
    public string? AttributeName { get; set; }

    [JsonPropertyName("attribute_value")]
    public string? AttributeValue { get; set; }

    [JsonPropertyName("index")]
    public int Index { get; set; } = -1;

    [JsonPropertyName("selector")]
    public string? Selector { get; set; }

    public bool FailIfMultiple { get; set; }
}
