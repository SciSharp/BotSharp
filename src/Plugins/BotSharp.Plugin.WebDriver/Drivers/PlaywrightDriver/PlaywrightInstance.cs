using System.IO;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
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

    public IPage GetPage(string id, string? pattern = null)
    {
        return _contexts[id].Pages.LastOrDefault();
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

        string tempFolderPath = $"{Path.GetTempPath()}\\playwright\\{ctxId}";

        _contexts[ctxId] = await _playwright.Chromium.LaunchPersistentContextAsync(tempFolderPath, new BrowserTypeLaunchPersistentContextOptions
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

    public async Task<IPage> NewPage(string ctxId, DataFetched? fetched)
    {
        var context = await GetContext(ctxId);
        var page = await context.NewPageAsync();

        // 许多网站为了防止信息被爬取，会添加一些防护手段。其中之一是检测 window.navigator.webdriver 属性。
        // 当使用 Playwright 打开浏览器时，该属性会被设置为 true，从而被网站识别为自动化工具。通过以下方式屏蔽这个属性，让网站无法识别是否使用了 Playwright
        var js = @"Object.defineProperties(navigator, {webdriver:{get:()=>false}});";
        await page.AddInitScriptAsync(js);

        if (fetched != null)
        {
            page.Response += async (sender, e) =>
            {
                if (e.Headers.ContainsKey("content-type") &&
                    e.Headers["content-type"].Contains("application/json") &&
                    e.Request.ResourceType == "fetch")
                {
                    Serilog.Log.Information($"Response: {e.Url}");
                    var json = await e.JsonAsync();
                    fetched(e.Url.ToLower(), JsonSerializer.Serialize(json));
                }
            };
        }

        return page;
    }

    /// <summary>
    /// Wait page and network until timeout in seconds
    /// </summary>
    /// <param name="ctxId"></param>
    /// <param name="timeout">seconds</param>
    /// <returns></returns>
    public async Task Wait(string ctxId, int timeout = 60)
    {
        foreach (var page in _pages[ctxId])
        {
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle, new PageWaitForLoadStateOptions
            {
                Timeout = 1000 * timeout
            });
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
