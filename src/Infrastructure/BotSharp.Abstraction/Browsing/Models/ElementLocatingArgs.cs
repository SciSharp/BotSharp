using System.Diagnostics;

namespace BotSharp.Abstraction.Browsing.Models;

[DebuggerStepThrough]
public class ElementLocatingArgs
{
    [JsonPropertyName("match_rule")]
    public string MatchRule { get; set; } = string.Empty;

    [JsonPropertyName("tag")]
    public string? Tag { get; set; } = null!;

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

    public bool Parent { get; set; }

    public bool FailIfMultiple { get; set; }
    public bool IgnoreIfNotFound { get; set; }

    /// <summary>
    /// Draw outline around the element
    /// </summary>
    public bool Highlight { get; set; }
    public string HighlightColor { get; set; } = "red";
}
