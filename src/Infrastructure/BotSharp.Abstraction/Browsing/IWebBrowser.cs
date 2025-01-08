using BotSharp.Abstraction.Browsing.Models;

namespace BotSharp.Abstraction.Browsing;

public interface IWebBrowser
{
    void SetServiceProvider (IServiceProvider services);
    Task<BrowserActionResult> LaunchBrowser(MessageInfo message, BrowserActionArgs args);
    Task<BrowserActionResult> ScreenshotAsync(MessageInfo message, string path);
    Task<BrowserActionResult> ScrollPage(MessageInfo message, PageActionArgs args);

    Task<BrowserActionResult> ActionOnElement(MessageInfo message, ElementLocatingArgs location, ElementActionArgs action);
    Task<BrowserActionResult> LocateElement(MessageInfo message, ElementLocatingArgs location);
    Task DoAction(MessageInfo message, ElementActionArgs action, BrowserActionResult result);
    Task PressKey(MessageInfo message, string key);

    Task<BrowserActionResult> InputUserText(BrowserActionParams actionParams);
    Task<BrowserActionResult> InputUserPassword(BrowserActionParams actionParams);
    Task<BrowserActionResult> ClickButton(BrowserActionParams actionParams);
    Task<BrowserActionResult> ClickElement(BrowserActionParams actionParams);
    Task<BrowserActionResult> ChangeListValue(BrowserActionParams actionParams);
    Task<BrowserActionResult> CheckRadioButton(BrowserActionParams actionParams);
    Task<BrowserActionResult> ChangeCheckbox(BrowserActionParams actionParams);
    Task<BrowserActionResult> GoToPage(MessageInfo message, PageActionArgs args);
    Task<string> ExtractData(BrowserActionParams actionParams);
    Task<T> EvaluateScript<T>(string contextId, string script);
    Task CloseBrowser(string contextId);
    Task<BrowserActionResult> CloseCurrentPage(MessageInfo message);
    Task<BrowserActionResult> SendHttpRequest(MessageInfo message, HttpRequestParams actionParams);
    Task<BrowserActionResult> GetAttributeValue(MessageInfo message, ElementLocatingArgs location);
    Task<BrowserActionResult> SetAttributeValue(MessageInfo message, ElementLocatingArgs location);
}
