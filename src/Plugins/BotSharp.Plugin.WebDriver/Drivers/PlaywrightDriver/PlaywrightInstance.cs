using System.IO;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
    Dictionary<string, IBrowserContext> _contexts = new Dictionary<string, IBrowserContext>();

    public IPage GetPage(string id)
    {
        InitInstance(id).Wait();
        return _contexts[id].Pages.LastOrDefault();
    }

    public async Task InitInstance(string id)
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();
        }
        await InitContext(id);
    }

    public async Task InitContext(string id)
    {
        if (_contexts.ContainsKey(id))
            return;

        string tempFolderPath = $"{Path.GetTempPath()}\\playwright\\{id}";
        _contexts[id] = await _playwright.Chromium.LaunchPersistentContextAsync(tempFolderPath, new BrowserTypeLaunchPersistentContextOptions
        {
            Headless = true,
            Channel = "chrome",
            IgnoreDefaultArgs = new[]
            {
                    "--disable-infobars"
                },
            Args = new[]
            {
                    "--disable-infobars",
                    // "--start-maximized"
                }
        });

        _contexts[id].Page += async (sender, e) =>
        {
            e.Close += async (sender, e) =>
            {
                Serilog.Log.Information($"Page is closed: {e.Url}");
            };
            Serilog.Log.Information($"New page is created: {e.Url}");
            await e.SetViewportSizeAsync(1280, 800);
        };

        _contexts[id].Close += async (sender, e) =>
        {
            Serilog.Log.Warning($"Playwright browser context is closed");
            _contexts.Remove(id);
        };
    }

    public async Task<IPage> NewPage(string id)
    {
        await InitContext(id);
        return await _contexts[id].NewPageAsync();
    }

    public async Task Wait(string id)
    {
        if (_contexts.ContainsKey(id))
        {
            await _contexts[id].Pages.Last().WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await _contexts[id].Pages.Last().WaitForLoadStateAsync(LoadState.NetworkIdle);
        }
        await Task.Delay(100);
    }

    public async Task Close(string id)
    {
        if (_contexts.ContainsKey(id))
        {
            await _contexts[id].CloseAsync();
        }
    }

    public void Dispose()
    {
        _contexts.Clear();
        _playwright.Dispose();
    }
}
