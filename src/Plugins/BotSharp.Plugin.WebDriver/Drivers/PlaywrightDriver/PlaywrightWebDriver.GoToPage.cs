namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, PageActionArgs args)
    {
        var result = new BrowserActionResult();
        var context = await _instance.GetContext(message.ContextId);
        try
        {
            var page = await _instance.NewPage(message, enableResponseCallback: args.EnableResponseCallback, 
                    responseInMemory: args.ResponseInMemory,
                    responseContainer: args.ResponseContainer,
                    excludeResponseUrls: args.ExcludeResponseUrls,
                    includeResponseUrls: args.IncludeResponseUrls);

            Serilog.Log.Information($"goto page: {args.Url}");

            if (args.OpenNewTab && page != null && page.Url == "about:blank")
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
                Timeout = args.Timeout > 0 ? args.Timeout : 30000
            });

            if (args.Selectors != null)
            {
                // 使用传入的选择器列表进行并行等待
                var tasks =args.Selectors.Select(selector =>
                    page.WaitForSelectorAsync(selector, new PageWaitForSelectorOptions
                    {
                        Timeout = args.Timeout > 0 ? args.Timeout : 30000
                    })
                ).ToArray();

                await Task.WhenAll(tasks);

                // 在此处提取所有选择器的 HTML 内容
                var contentTasks = args.Selectors.Select(selector => page.InnerHTMLAsync(selector)).ToArray();
                var contents = await Task.WhenAll(contentTasks);

                result.IsSuccess = true;
                result.Body = string.Join(", ", contents.Select((content, index) => $"{args.Selectors[index]}: {content}"));

                return result;
            }

            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            if (args.WaitForNetworkIdle)
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = args.Timeout > 0 ? args.Timeout : 30000
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
