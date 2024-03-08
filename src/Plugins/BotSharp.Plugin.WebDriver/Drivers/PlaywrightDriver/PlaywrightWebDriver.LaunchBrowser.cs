namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(string conversationId, string? url)
    {
        var result = new BrowserActionResult() 
        { 
            IsSuccess = true 
        };
        await _instance.InitInstance(conversationId);

        if (!string.IsNullOrEmpty(url))
        {
            var page = await _instance.NewPage(conversationId);
            
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
                    result.ErrorMessage = ex.Message;
                    result.StackTrace = ex.StackTrace;
                    _logger.LogError(ex.Message);
                }
            }
        }

        return result;
    }
}
