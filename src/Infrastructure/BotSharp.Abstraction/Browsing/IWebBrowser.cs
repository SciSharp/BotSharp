using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebBrowser
{
    Task<BrowserActionResult> LaunchBrowser(string conversationId, string? url);
    Task<BrowserActionResult> ScreenshotAsync(string conversationId, string path);
    Task<BrowserActionResult> ScrollPageAsync(BrowserActionParams actionParams);

    Task<BrowserActionResult> ActionOnElement(MessageInfo message, ElementLocatingArgs location, ElementActionArgs action);
    Task<BrowserActionResult> LocateElement(MessageInfo message, ElementLocatingArgs location);
    Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result);

    Task<BrowserActionResult> InputUserText(BrowserActionParams actionParams);
    Task<BrowserActionResult> InputUserPassword(BrowserActionParams actionParams);
    Task<BrowserActionResult> ClickButton(BrowserActionParams actionParams);
    Task<BrowserActionResult> ClickElement(BrowserActionParams actionParams);
    Task<BrowserActionResult> ChangeListValue(BrowserActionParams actionParams);
    Task<BrowserActionResult> CheckRadioButton(BrowserActionParams actionParams);
    Task<BrowserActionResult> ChangeCheckbox(BrowserActionParams actionParams);
    Task<BrowserActionResult> GoToPage(string conversationId, string url);
    Task<string> ExtractData(BrowserActionParams actionParams);
    Task<T> EvaluateScript<T>(string conversationId, string script);
    Task CloseBrowser(string conversationId);
    Task<BrowserActionResult> SendHttpRequest(HttpRequestParams actionParams);
    Task<string> GetAttributeValue(MessageInfo message, ElementLocatingArgs location, BrowserActionResult result);
}
