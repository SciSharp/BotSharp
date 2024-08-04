namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(MessageInfo message, BrowserActionArgs args)
    {
        var context = await _instance.InitContext(message.ContextId, args);
        var result = new BrowserActionResult
        {
            IsSuccess = context.Pages.Count > 0
        };

        return result;
    }
}
