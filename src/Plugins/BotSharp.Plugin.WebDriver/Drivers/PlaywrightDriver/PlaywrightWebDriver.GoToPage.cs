using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> GoToPage(string url)
    {
        try
        {
            var response = await _instance.Page.GotoAsync(url);
            await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

            return response.Status == 200;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        
        return false;
    }
}
