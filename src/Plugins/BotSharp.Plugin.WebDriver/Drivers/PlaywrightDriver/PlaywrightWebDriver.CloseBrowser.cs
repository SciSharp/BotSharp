namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseBrowser(string contextId)
    {
        await _instance.Close(contextId);
    }
}
