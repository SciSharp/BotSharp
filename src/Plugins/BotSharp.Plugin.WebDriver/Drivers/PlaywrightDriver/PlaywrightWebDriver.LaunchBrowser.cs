namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(string contextId, string? url, bool openIfNotExist = true)
    {
        var result = new BrowserActionResult() 
        { 
            IsSuccess = true 
        };
        var context = await _instance.InitInstance(contextId);

        if (!string.IsNullOrEmpty(url))
        {
            // Check if the page is already open
            foreach (var p in context.Pages)
            {
                if (p.Url == url)
                {
                    await p.BringToFrontAsync();
                    return result;
                }
            }

            var page = await _instance.NewPage(contextId);
            
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    var response = await page.GotoAsync(url, new PageGotoOptions
                    {
                        Timeout = 15 * 1000
                    });
                    await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
                    result.IsSuccess = response.Status == 200;
                }
                catch(Exception ex)
                {
                    result.Message = ex.Message;
                    result.StackTrace = ex.StackTrace;
                    _logger.LogError(ex.Message);
                }
            }
        }

        return result;
    }
}
