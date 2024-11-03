using BotSharp.Abstraction.Browsing.Enums;
using System.Diagnostics;

namespace BotSharp.Abstraction.Browsing.Models;

[DebuggerStepThrough]
public class PageActionArgs
{
    public BroswerActionEnum Action { get; set; }

    public string? Content { get; set; }
    public string Direction { get; set; } = "down";

    public string Url { get; set; } = null!;

    /// <summary>
    /// This value has to be set to true if you want to get the page XHR/ Fetch responses
    /// </summary>
    public bool OpenNewTab { get; set; } = false;

    public bool EnableResponseCallback { get; set; } = false;

    /// <summary>
    /// Exclude urls for XHR/ Fetch responses
    /// </summary>
    public string[]? ExcludeResponseUrls { get; set; }

    /// <summary>
    /// Only include urls for XHR/ Fetch responses
    /// </summary>
    public string[]? IncludeResponseUrls { get; set; }

    public List<string>? Selectors { get; set; }

    /// <summary>
    /// If set to true, the response will be stored in memory
    /// </summary>
    public bool ResponseInMemory { get; set; } = false;
    public List<WebPageResponseData>? ResponseContainer { get; set; }

    public bool UseExistingPage { get; set; } = false;

    public bool WaitForNetworkIdle { get; set; } = true;
    public float? Timeout { get; set; }

    /// <summary>
    /// Wait time in seconds after page is opened
    /// </summary>
    public int WaitTime { get; set; }

    public bool ReadInnerHTMLAsBody { get; set; } = false;
}
