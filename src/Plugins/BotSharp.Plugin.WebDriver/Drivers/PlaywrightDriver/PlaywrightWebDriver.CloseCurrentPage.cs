namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> CloseCurrentPage(MessageInfo message)
    {
        await _instance.CloseCurrentPage(message.ContextId);
        var result = new BrowserActionResult
        {
            IsSuccess = true
        };

        return result;
    }
}
