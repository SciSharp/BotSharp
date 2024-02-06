using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseBrowser()
    {
        // await _instance.Browser.CloseAsync();
        _logger.LogInformation($"Closed browser with page {_instance.Page.Url}");
    }
}
