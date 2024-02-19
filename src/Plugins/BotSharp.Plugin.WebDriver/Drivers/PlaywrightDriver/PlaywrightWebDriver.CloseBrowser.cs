namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseBrowser(string conversationId)
    {
        await _instance.Close(conversationId);
    }
}
