namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(string contextId, string url, bool openNewTab = false)
    {
        var result = new BrowserActionResult();
        try
        {
            var page = openNewTab ? await _instance.NewPage(contextId) : 
                _instance.GetPage(contextId);
            var response = await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (response.Status == 200)
            {
                result.Body = await page.ContentAsync();
                result.IsSuccess = true;
            }
            else
            {
                result.Message = response.StatusText;
            }
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }
        
        return result;
    }
}
