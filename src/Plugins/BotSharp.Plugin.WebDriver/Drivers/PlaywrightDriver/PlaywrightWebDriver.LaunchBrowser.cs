namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(MessageInfo message)
    {
        var context = await _instance.InitInstance(message.ContextId);
        var result = new BrowserActionResult
        {
            IsSuccess = context.Pages.Count > 0
        };

        return result;
    }
}
