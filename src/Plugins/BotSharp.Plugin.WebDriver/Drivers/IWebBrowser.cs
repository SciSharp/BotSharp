namespace BotSharp.Plugin.WebDriver.Drivers;

public interface IWebBrowser
{
    Task<bool> LaunchBrowser(string conversationId, string? url);
    Task<string> ScreenshotAsync(string conversationId, string path);
    Task<bool> ScrollPageAsync(BrowserActionParams actionParams);
    Task<bool> InputUserText(BrowserActionParams actionParams);
    Task<bool> InputUserPassword(BrowserActionParams actionParams);
    Task<bool> ClickButton(BrowserActionParams actionParams);
    Task<bool> ClickElement(BrowserActionParams actionParams);
    Task<bool> ChangeListValue(BrowserActionParams actionParams);
    Task<bool> CheckRadioButton(BrowserActionParams actionParams);
    Task<bool> ChangeCheckbox(BrowserActionParams actionParams);
    Task<bool> GoToPage(string conversationId, string url);
    Task<string> ExtractData(BrowserActionParams actionParams);
    Task<T> EvaluateScript<T>(string conversationId, string script);
    Task CloseBrowser(string conversationId);
}
