using BotSharp.Abstraction.Browsing.Enums;
using System.Diagnostics;

namespace BotSharp.Abstraction.Browsing.Models;

[DebuggerStepThrough]
public class ElementActionArgs
{
    [JsonPropertyName("action")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public BroswerActionEnum Action { get; set; }

    [JsonPropertyName("content")]
    public string? Content { get; set; }
    [JsonPropertyName("file_urls")] 
    public string[] FileUrl { get; set; }

    public ElementPosition? Position { get; set; }

    /// <summary>
    /// Delay milliseconds before pressing key
    /// </summary>
    public int DelayBeforePressingKey { get; set; }

    [JsonPropertyName("press_key")]
    public string? PressKey { get; set; }

    /// <summary>
    /// Locator option
    /// </summary>
    public bool FirstIfMultipleFound { get; set; } = false;

    /// <summary>
    /// Wait time in seconds
    /// </summary>
    [JsonPropertyName("wait_time")]
    public int WaitTime { get; set; }

    /// <summary>
    /// Add current url to the content
    /// </summary>
    [JsonPropertyName("show_current_url")]
    public bool ShowCurrentUrl { get; set; } = false;

    /// <summary>
    /// Required for deserialization
    /// </summary>
    public ElementActionArgs()
    {

    }

    public ElementActionArgs(BroswerActionEnum action, ElementPosition? position = null)
    {
        Action = action;
        Position = position;
    }

    public ElementActionArgs(BroswerActionEnum action, string content)
    {
        Action = action;
        Content = content;
    }
}
