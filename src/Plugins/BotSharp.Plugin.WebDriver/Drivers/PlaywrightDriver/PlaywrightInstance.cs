namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
    IBrowser _browser;
    IPage _page;

    // public IPlaywright Playwright => _playwright;
    public IBrowser Browser => _browser;
    public IPage Page => _page;

    public async Task InitInstance()
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();

            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",
                Args = new[] 
                { 
                    "--start-maximized" 
                }
            });
        }
    }

    public void SetPage(IPage page) { _page = page; }

    public void Dispose()
    {
        _playwright.Dispose();
    }
}
