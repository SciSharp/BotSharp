using BotSharp.Abstraction.Browsing.Enums;

namespace BotSharp.Abstraction.Browsing.Models;

public class PageActionArgs
{
    public BroswerActionEnum Action { get; set; }

    public string? Content { get; set; }
    public string? Direction { get; set; }

    public string Url { get; set; } = null!;

    /// <summary>
    /// This value has to be set to true if you want to get the page XHR/ Fetch responses
    /// </summary>
    public bool OpenNewTab { get; set; } = false;
    /// <summary>
    /// Exclude urls for XHR/ Fetch responses
    /// </summary>
    public string[]? ExcludeResponseUrls { get; set; }
    public bool UseExistingPage { get; set; } = false;

    public bool WaitForNetworkIdle { get; set; } = true;
    public float? Timeout { get; set; }

    /// <summary>
    /// Wait time in seconds after page is opened
    /// </summary>
    public int WaitTime { get; set; }
}
