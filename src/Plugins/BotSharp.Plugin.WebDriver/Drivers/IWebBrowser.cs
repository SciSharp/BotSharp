namespace BotSharp.Plugin.WebDriver.Drivers;

public interface IWebBrowser
{
    Agent Agent { get; }
    void SetAgent(Agent agent);
    Task LaunchBrowser(string? url);
}
