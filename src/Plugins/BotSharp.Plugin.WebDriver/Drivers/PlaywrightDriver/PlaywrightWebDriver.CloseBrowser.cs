using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task CloseBrowser(Agent agent, BrowsingContextIn context, string messageId)
    {
        // await _instance.Browser.CloseAsync();
        _logger.LogInformation($"{agent.Name} closed browser with page {_instance.Page.Url}");
    }
}
