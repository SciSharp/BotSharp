namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, PageActionArgs args)
    {
        var result = new BrowserActionResult();
        var context = await _instance.GetContext(message.ContextId);
        try
        {
            var page = args.UseExistingPage ? 
                _instance.GetPage(message.ContextId, pattern: args.Url) :
                await _instance.NewPage(message);

            if (args.UseExistingPage && page != null && page.Url != "about:blank")
            {
                Serilog.Log.Information($"goto existing page: {args.Url}");
                result.IsSuccess = true;
                return result;
            }

            Serilog.Log.Information($"goto page: {args.Url}");

            if (args.UseExistingPage && args.OpenNewTab && page != null && page.Url == "about:blank")
            {
                page = await _instance.NewPage(message);
            }

            var response = await page.GotoAsync(args.Url, new PageGotoOptions
            {
                Timeout = args.Timeout
            });

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            if (args.WaitForNetworkIdle)
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = args.Timeout
                });
            }

            if (args.WaitTime > 0)
            {
                await Task.Delay(args.WaitTime * 1000);
            }

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
