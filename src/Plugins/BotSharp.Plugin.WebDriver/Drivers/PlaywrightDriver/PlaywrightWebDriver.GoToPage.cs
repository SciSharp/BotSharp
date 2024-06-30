using System.Linq;

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

            var page = args.OpenNewTab ? await _instance.NewPage(message.ContextId, fetched: args.OnDataFetched) : 
                _instance.GetPage(message.ContextId);

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

    private void Page_Response1(object sender, IResponse e)
    {
        throw new NotImplementedException();
    }

    private void Page_Response(object sender, IResponse e)
    {
        throw new NotImplementedException();
    }
}
