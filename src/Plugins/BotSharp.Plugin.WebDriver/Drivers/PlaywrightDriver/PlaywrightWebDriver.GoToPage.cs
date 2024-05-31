namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, string url, bool openNewTab = false)
    {
        var result = new BrowserActionResult();
        var context = await _instance.InitInstance(message.ContextId);
        try
        {
            // Check if the page is already open
            if (!openNewTab && context.Pages.Count > 0)
            {
                foreach (var p in context.Pages)
                {
                    if (p.Url == url)
                    {
                        result.Body = await p.ContentAsync();
                        result.IsSuccess = true;
                        await p.BringToFrontAsync();
                        return result;
                    }
                }
            }

            var page = openNewTab ? await _instance.NewPage(message.ContextId) : 
                _instance.GetPage(message.ContextId);
            var response = await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 1000 * 60 * 5
            });

            if (response.Status == 200)
            {
                // Disable this due to performance issue, some page is too large
                // result.Body = await page.InnerHTMLAsync("body");
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
