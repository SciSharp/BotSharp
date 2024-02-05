namespace BotSharp.Plugin.WebDriver.Drivers;

public interface IWebBrowser
{
    Task LaunchBrowser(string? url);
    Task InputUserText(Agent agent, BrowsingContextIn context, string messageId);
    Task InputUserPassword(Agent agent, BrowsingContextIn context, string messageId);
    Task ClickElement(Agent agent, BrowsingContextIn context, string messageId);
    Task GoToPage(Agent agent, BrowsingContextIn context, string messageId);
}
