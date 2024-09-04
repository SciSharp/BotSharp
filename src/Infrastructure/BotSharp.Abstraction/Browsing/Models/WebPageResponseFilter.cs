namespace BotSharp.Abstraction.Browsing.Models;

public class WebPageResponseFilter
{
    public string Url { get; set; } = null!;
    public string[]? QueryParameters { get; set; }
}
