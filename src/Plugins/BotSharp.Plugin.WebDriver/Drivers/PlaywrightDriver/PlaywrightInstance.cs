using Microsoft.Playwright;
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
        InitInstance(id).Wait();
        return _contexts[id].Pages.LastOrDefault();
    }

    public async Task<IBrowserContext> InitInstance(string ctxId)
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();
        }
        return await InitContext(ctxId);
    }

    public async Task<IBrowserContext> InitContext(string ctxId)
    {
        if (_contexts.ContainsKey(ctxId))
            return _contexts[ctxId];

        string tempFolderPath = $"{Path.GetTempPath()}\\playwright\\{ctxId}";

        _contexts[ctxId] = await _playwright.Chromium.LaunchPersistentContextAsync(tempFolderPath, new BrowserTypeLaunchPersistentContextOptions
        {
#if DEBUG
            Headless = false,
#else
            Headless = true,
#endif
            Channel = "chrome",
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
            await page.SetViewportSizeAsync(1280, 800);
        };

        _contexts[ctxId].Close += async (sender, e) =>
        {
            Serilog.Log.Warning($"Playwright browser context is closed");
            _pages.Remove(ctxId);
            _contexts.Remove(ctxId);
        };

        return _contexts[ctxId];
    }

    public async Task<IPage> NewPage(string ctxId)
    {
        await InitContext(ctxId);
        var page = await _contexts[ctxId].NewPageAsync();
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
            await _pages[ctxId][i].CloseAsync();
        }
    }

    public void Dispose()
    {
        _contexts.Clear();
        _playwright?.Dispose();
    }
}
