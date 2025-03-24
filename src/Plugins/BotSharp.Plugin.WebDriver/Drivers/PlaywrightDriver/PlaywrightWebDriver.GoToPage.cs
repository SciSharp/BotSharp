namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, PageActionArgs args)
    {
        var result = new BrowserActionResult();
        var context = await _instance.GetContext(message.ContextId);
        try
        {
            IPage? page = null;
            if (!args.OpenNewTab)
            {
                page = _instance.Contexts[message.ContextId].Pages.LastOrDefault();

                if (page != null)
                {
                    if (!args.OpenBlankPage)
                    {
                        if (!_instance.Pages[message.ContextId].Contains(page))
                        {
                            _instance.Pages[message.ContextId].Add(page);
                        }
                    }
                    if (args.OpenBlankPage && page.Url != "about:blank")
                    {
                        await page.EvaluateAsync(@"() => {
                            window.open('', '_blank');
                        }");
                    }

                    if (args.EnableResponseCallback)
                    {
                        page.Request += async (sender, e) =>
                        {
                            await _instance.HandleFetchRequest(e, message, args);
                        };

                        page.Response += async (sender, e) =>
                        {
                            await _instance.HandleFetchResponse(e, message, args);
                        };
                    }
                }
            }
            else
            {
                page = await _instance.NewPage(message, args);
            }

            if (page == null)
            {
                page = await _instance.NewPage(message, args);
            }

            // Active current tab
            await page.BringToFrontAsync();
            var response = await page.GotoAsync(args.Url, new PageGotoOptions
            {
                Timeout = args.Timeout > 0 ? args.Timeout : 30000
            });

            if (args.Selectors != null)
            {
                // 使用传入的选择器列表进行并行等待
                var tasks = args.Selectors.Select(selector =>
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
            result.UrlAfterAction = page.Url;
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
