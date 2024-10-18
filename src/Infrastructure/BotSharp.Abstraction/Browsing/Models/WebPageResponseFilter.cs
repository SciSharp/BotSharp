namespace BotSharp.Abstraction.Browsing.Models;

public class WebPageResponseFilter
{
    public string Url { get; set; } = null!;
    public string[]? QueryParameters { get; set; }

    /// <summary>
    /// contains, starts, ends, equals
    /// </summary>
    public string UrlMatchPattern { get; set; } = "contains";

    /// <summary>
    /// Handle Content-Type: text/x-component
    /// </summary>
    public int PartIndex { get; set; } = -1;
}
