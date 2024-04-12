namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ActionOnElement(MessageInfo message, ElementLocatingArgs location, ElementActionArgs action)
    {
        await _instance.Wait(message.ContextId);
        var result = await LocateElement(message, location);
        if (result.IsSuccess)
        {
            await DoAction(message, action, result);
        }
        return result;
    }
}
