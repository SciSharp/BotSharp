namespace BotSharp.Abstraction.Browsing.Models;

public class BrowserActionArgs
{
    public bool Headless { get; set; }
    public string? UserDataDir { get; set; }
    public string? RemoteHostUrl { get; set; }
}
