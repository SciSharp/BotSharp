
namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseBrowser()
    {
        if (_instance.Context != null)
        {
            await _instance.Context.CloseAsync();
        }
    }
}
