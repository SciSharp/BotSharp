using System.IO;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
    IServiceProvider _services;
    public IServiceProvider Services => _services;
    Dictionary<string, IBrowserContext> _contexts = new Dictionary<string, IBrowserContext>();
    Dictionary<string, List<IPage>> _pages = new Dictionary<string, List<IPage>>();

    /// <summary>
    /// ContextId and BrowserContext
    /// </summary>
    public Dictionary<string, IBrowserContext> Contexts => _contexts;

    /// <summary>
    /// ContextId and Pages
    /// </summary>
    public Dictionary<string, List<IPage>> Pages => _pages;

    public void SetServiceProvider(IServiceProvider services)
    {
        _services = services;
    }

    public IPage? GetPage(string contextId)
    {
        return _contexts[contextId].Pages.LastOrDefault();
    }

    public async Task<IBrowserContext> GetContext(string ctxId)
    {
        return _contexts[ctxId];
    }

    public async Task<IBrowserContext> InitContext(string ctxId, BrowserActionArgs args)
    {
        if (_contexts.ContainsKey(ctxId))
            return _contexts[ctxId];

        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();
        }

        if (!string.IsNullOrEmpty(args.RemoteHostUrl))
        {
            var browser = await _playwright.Chromium.ConnectOverCDPAsync(args.RemoteHostUrl);
            _contexts[ctxId] = browser.Contexts[0];
        }
        else
        {
            string userDataDir = args.UserDataDir ?? $"{Path.GetTempPath()}\\playwright\\{ctxId}";
            _contexts[ctxId] = await _playwright.Chromium.LaunchPersistentContextAsync(userDataDir, new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = args.Headless,
                Channel = "chrome",
                ViewportSize = new ViewportSize
                {
                    Width = 1600,
                    Height = 900
                },
                IgnoreDefaultArgs =
                [
                    "--enable-automation",
                ],
                Args =
                [
                    "--disable-infobars",
                    "--test-type"
                    // "--start-maximized"
                ]
            });
        }

        _pages[ctxId] = new List<IPage>();

        _contexts[ctxId].Page += async (sender, page) =>
        {
            _pages[ctxId].Add(page);
            page.Close += async (sender, e) =>
            {
                _pages[ctxId].Remove(e);
                Serilog.Log.Information($"Page is closed: {e.Url}");
            };
            Serilog.Log.Information($"New page is created: {page.Url}");

            /*page.Response += async (sender, e) =>
            {
                Serilog.Log.Information($"Response: {e.Url}");
                if (e.Headers.ContainsKey("content-type") && e.Headers["content-type"].Contains("application/json"))
                {
                    var json = await e.JsonAsync();
                    Serilog.Log.Information(json.ToString());
                }
            };*/
        };

        _contexts[ctxId].Close += async (sender, e) =>
        {
            Serilog.Log.Warning($"Playwright browser context is closed");
            _pages.Remove(ctxId);
            _contexts.Remove(ctxId);
        };

        return _contexts[ctxId];
    }

    public async Task<IPage> NewPage(MessageInfo message, PageActionArgs args)
    {
        var context = await GetContext(message.ContextId);
        var page = await context.NewPageAsync();

        // 许多网站为了防止信息被爬取，会添加一些防护手段。其中之一是检测 window.navigator.webdriver 属性。
        // 当使用 Playwright 打开浏览器时，该属性会被设置为 true，从而被网站识别为自动化工具。通过以下方式屏蔽这个属性，让网站无法识别是否使用了 Playwright
        var js = @"Object.defineProperties(navigator, {webdriver:{get:()=>false}});";
        await page.AddInitScriptAsync(js);

        if (!args.EnableResponseCallback)
        {
            return page;
        }

        page.Response += async (sender, e) =>
        {
            await HandleFetchResponse(e, message, args);
        };

        return page;
    }

    public async Task HandleFetchResponse(IResponse response, MessageInfo message, PageActionArgs args)
    {
        if (response.Status != 204 &&
                        response.Headers.ContainsKey("content-type") &&
                        (response.Request.ResourceType == "fetch" || response.Request.ResourceType == "xhr") &&
                        (args.ExcludeResponseUrls == null || !args.ExcludeResponseUrls.Any(url => response.Url.ToLower().Contains(url))) &&
                        (args.IncludeResponseUrls == null || args.IncludeResponseUrls.Any(url => response.Url.ToLower().Contains(url))))
        {
            Serilog.Log.Information($"{response.Request.Method}: {response.Url}");

            try
            {
                var result = new WebPageResponseData
                {
                    Url = response.Url.ToLower(),
                    PostData = response.Request?.PostData ?? string.Empty,
                    ResponseInMemory = args.ResponseInMemory
                };

                var html = await response.TextAsync();
                if (response.Headers["content-type"].Contains("application/json"))
                {
                    if (response.Status == 200 && response.Ok)
                    {                        
                        if (!string.IsNullOrWhiteSpace(html))
                        {
                            var json = await response.JsonAsync();
                            result.ResponseData = JsonSerializer.Serialize(json);
                        }
                    }
                }
                else
                {
                    result.ResponseData = html;
                }

                if (args.ResponseContainer != null && args.ResponseInMemory)
                {
                    args.ResponseContainer.Add(result);
                }

                Serilog.Log.Warning($"Response status: {response.Status} {response.StatusText}, OK: {response.Ok}");
                var webPageResponseHooks = _services.GetServices<IWebPageResponseHook>();
                foreach (var hook in webPageResponseHooks)
                {
                    hook.OnDataFetched(message, result);
                }
            }
            catch (ObjectDisposedException ex)
            {
                Serilog.Log.Information(ex.Message);
            }
            catch (Exception ex)
            {
                Serilog.Log.Error($"{response.Url}\r\n" + ex.ToString());
            }
        }
    }

    /// <summary>
    /// Wait page and network until timeout in seconds
    /// </summary>
    /// <param name="ctxId"></param>
    /// <param name="timeout">seconds</param>
    /// <returns></returns>
    public async Task Wait(string ctxId, int timeout = 10, bool waitNetworkIdle = true)
    {
        foreach (var page in _pages[ctxId])
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            if (waitNetworkIdle)
            {
                await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
                {
                    Timeout = 1000 * timeout
                });
            }
        }
        await Task.Delay(100);
    }

    public async Task Close(string ctxId)
    {
        if (_contexts.ContainsKey(ctxId))
        {
            await _contexts[ctxId].CloseAsync();
        }
    }

    public async Task CloseCurrentPage(string ctxId)
    {
        var pages = _pages[ctxId].ToArray();
        for (var i = 0; i < pages.Length; i++)
        {
            var page = _pages[ctxId].FirstOrDefault();
            if (page != null)
            {
                await page.CloseAsync();
            }
        }
    }

    public void Dispose()
    {
        _contexts.Clear();
        _playwright?.Dispose();
    }
}
