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
                await _instance.NewPage(message, enableResponseCallback: args.EnableResponseCallback, 
                    responseInMemory: args.ResponseInMemory,
                    responseContainer: args.ResponseContainer,
                    excludeResponseUrls: args.ExcludeResponseUrls,
                    includeResponseUrls: args.IncludeResponseUrls);

            if (args.UseExistingPage && page != null && page.Url == args.Url)
            {
                Serilog.Log.Information($"goto existing page: {args.Url}");
                result.IsSuccess = true;
                return result;
            }

            Serilog.Log.Information($"goto page: {args.Url}");

            if (args.UseExistingPage && args.OpenNewTab && page != null && page.Url == "about:blank")
            {
                page = await _instance.NewPage(message, 
                    enableResponseCallback: args.EnableResponseCallback,
                    responseInMemory: args.ResponseInMemory,
                    responseContainer: args.ResponseContainer,
                    excludeResponseUrls: args.ExcludeResponseUrls,
                    includeResponseUrls: args.IncludeResponseUrls);
            }

            if (page == null)
            {
                page = await _instance.NewPage(message, 
                    enableResponseCallback: args.EnableResponseCallback,
                    responseInMemory: args.ResponseInMemory,
                    responseContainer: args.ResponseContainer,
                    excludeResponseUrls: args.ExcludeResponseUrls,
                    includeResponseUrls: args.IncludeResponseUrls);
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

            result.ResponseStatusCode = response.Status;
            if (response.Status == 200)
            {
                result.IsSuccess = true;

                // Be careful if page is too large, it will cause performance issue
                if (args.ReadInnerHTMLAsBody)
                {
                    result.Body = await page.InnerHTMLAsync("body");
                }
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
