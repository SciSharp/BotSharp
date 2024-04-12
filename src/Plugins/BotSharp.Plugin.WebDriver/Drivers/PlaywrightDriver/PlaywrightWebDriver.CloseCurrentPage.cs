namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseCurrentPage(string contextId)
    {
        await _instance.CloseCurrentPage(contextId);
    }
}
