namespace BotSharp.Abstraction.Browsing.Settings;

public class WebBrowsingSettings
{
    public string Driver { get; set; } = "Playwright";
    public bool Headless { get; set; }
    public float? DefaultTimeOut { get; set; }
}
