using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Abstraction.Browsing.Models;

public class PageActionArgs
{
    public BroswerActionEnum Action { get; set; }

    public string? Content { get; set; }
    public string? Direction { get; set; }

    public string Url { get; set; } = null!;
    public bool OpenNewTab { get; set; } = false;
}
