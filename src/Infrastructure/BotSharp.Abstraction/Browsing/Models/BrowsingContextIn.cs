namespace BotSharp.Abstraction.Browsing.Models;

public class BrowsingContextIn
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("element_name")]
    public string? ElementName { get; set; }

    [JsonPropertyName("element_type")]
    public string? ElementType { get; set; }

    [JsonPropertyName("input_text")]
    public string? InputText { get; set; }

    [JsonPropertyName("element_text")]
    public string? ElementText { get; set; }

    [JsonPropertyName("attribute_name")]
    public string? AttributeName { get; set; }

    [JsonPropertyName("attribute_value")]
    public string? AttributeValue { get; set; }

    [JsonPropertyName("press_enter")]
    public bool? PressEnter { get; set; }

    [JsonPropertyName("match_rule")]
    public string MatchRule { get; set; } = string.Empty;

    [JsonPropertyName("update_value")]
    public string? UpdateValue { get; set; }

    [JsonPropertyName("password")]
    public string? Password { get; set; }

    [JsonPropertyName("question")]
    public string? Question { get; set; }

    [JsonPropertyName("direction")]
    public string? Direction { get; set; }
}
