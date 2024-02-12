using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> GoToPage(string conversationId, string url)
    {
        try
        {
            var response = await _instance.GetPage(conversationId).GotoAsync(url);
            await _instance.GetPage(conversationId).WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await _instance.GetPage(conversationId).WaitForLoadStateAsync(LoadState.NetworkIdle);

            return response.Status == 200;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }
        
        return false;
    }
}
