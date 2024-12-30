namespace BotSharp.Plugin.Selenium.Drivers;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(MessageInfo message, BrowserActionArgs args)
    {
        var context = await _instance.InitInstance(message.ContextId);
        var result = new BrowserActionResult()
        {
            IsSuccess = true
        };
        return result;
    }
}
