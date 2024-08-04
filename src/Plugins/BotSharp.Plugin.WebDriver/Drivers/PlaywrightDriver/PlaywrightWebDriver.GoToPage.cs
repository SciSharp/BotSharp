namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, PageActionArgs args)
    {
        var result = new BrowserActionResult();
        var context = await _instance.GetContext(message.ContextId);
        try
        {
            // Check if the page is already open
            /*if (!args.OpenNewTab && context.Pages.Count > 0)
            {
                foreach (var p in context.Pages)
                {
                    if (p.Url == args.Url)
                    {
                        // Disable this due to performance issue, some page is too large
                        // result.Body = await p.ContentAsync();
                        result.IsSuccess = true;
                        // await p.BringToFrontAsync();
                        return result;
                    }
                }
            }*/

            var page = args.OpenNewTab ? await _instance.NewPage(message, _services) : 
                _instance.GetPage(message.ContextId, pattern: args.Url);

            page.Response += async (sender, e) =>
            {
                if (e.Headers.ContainsKey("content-type") &&
                    e.Headers["content-type"].Contains("application/json") &&
                    (e.Request.ResourceType == "fetch" || e.Request.ResourceType == "xhr"))
                {
                    Serilog.Log.Information($"{e.Request.Method}: {e.Url}");
                    JsonElement? json = null;
                    try
                    {
                        if (e.Status == 200 && e.Ok)
                        {
                            json = await e.JsonAsync();
                        }
                        else
                        {
                            Serilog.Log.Warning($"Response status: {e.Status} {e.StatusText}, OK: {e.Ok}");
                        }

                        var webPageResponseHooks = _services.GetServices<IWebPageResponseHook>();
                        foreach (var hook in webPageResponseHooks)
                        {
                            hook.OnDataFetched(message, e.Url.ToLower(), e.Request?.PostData ?? string.Empty, JsonSerializer.Serialize(json));
                        }
                    }
                    catch (Exception ex)
                    {
                        Serilog.Log.Error(ex.ToString());
                    }
                }
            };

            if (!args.OpenNewTab && page != null && page.Url != "about:blank")
            {
                Serilog.Log.Information($"goto existing page: {args.Url}");
                result.IsSuccess = true;
                return result;
            }

            Serilog.Log.Information($"goto page: {args.Url}");

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
