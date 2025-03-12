namespace BotSharp.Abstraction.Browsing.Settings;

public class WebBrowsingSettings
{
    public string Driver { get; set; } = "Playwright";
    public bool Headless { get; set; }
    // Default timeout in milliseconds
    public float DefaultTimeout { get; set; } = 30000;
    public bool IsEnableScreenshot { get; set; }
}
