using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> LaunchBrowser(string? url)
    {
        await _instance.InitInstance();

        if (!string.IsNullOrEmpty(url))
        {
            var page = _instance.Context.Pages.LastOrDefault();
            if (page == null)
            {
                page = await _instance.Context.NewPageAsync();
            }
            
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var response = await page.GotoAsync(url, new PageGotoOptions
                    {
                        Timeout = 15 * 1000
                    });
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    return response.Status == 200;
                }
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                }
                return false;
            }
        }

        return true;
    }
}
