namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(string conversationId, string url)
    {
        var result = new BrowserActionResult();
        try
        {
            var response = await _instance.GetPage(conversationId).GotoAsync(url);
            await _instance.GetPage(conversationId).WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await _instance.GetPage(conversationId).WaitForLoadStateAsync(LoadState.NetworkIdle);

            if (response.Status == 200)
            {
                var page = _instance.GetPage(conversationId);
                result.Body = await page.ContentAsync();
                result.IsSuccess = true;
            }
            else
            {
                result.ErrorMessage = response.StatusText;
            }
        }
        catch (Exception ex)
        {
            result.ErrorMessage = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }
        
        return result;
    }
}
