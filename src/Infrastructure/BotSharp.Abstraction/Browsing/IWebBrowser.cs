using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebBrowser
{
    Task<BrowserActionResult> LaunchBrowser(string contextId, string? url, bool openIfNotExist = true);
    Task<BrowserActionResult> ScreenshotAsync(string contextId, string path);
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
    Task<BrowserActionResult> GoToPage(string contextId, string url, bool openNewTab = false);
    Task<string> ExtractData(BrowserActionParams actionParams);
    Task<T> EvaluateScript<T>(string contextId, string script);
    Task CloseBrowser(string contextId);
    Task CloseCurrentPage(string contextId);
    Task<BrowserActionResult> SendHttpRequest(MessageInfo message, HttpRequestParams actionParams);
    Task<BrowserActionResult> GetAttributeValue(MessageInfo message, ElementLocatingArgs location);
}
