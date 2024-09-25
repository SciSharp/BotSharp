using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Abstraction.Browsing.Models;

public class ElementActionArgs
{
    public BroswerActionEnum Action { get; set; }

    public string? Content { get; set; }

    public ElementPosition? Position { get; set; }

    public string? PressKey { get; set; }

    /// <summary>
    /// Locator option
    /// </summary>
    public bool FirstIfMultipleFound { get; set; } = false;

    /// <summary>
    /// Wait time in seconds
    /// </summary>
    public int WaitTime { get; set; }

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
