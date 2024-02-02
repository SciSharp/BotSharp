namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
    IBrowser _browser;
    IBrowserContext _context;
    IPage _page;

    // public IPlaywright Playwright => _playwright;
    public IBrowser Browser => _browser;
    public IBrowserContext Context => _context;
    public IPage Page => _page;

    public async Task InitInstance()
    {
        if (_playwright == null)
        {
            _playwright = await Playwright.CreateAsync();

            /*_browser = await _playwright.Chromium.LaunchPersistentContextAsync(@"C:\Users\haipi\AppData\Local\Google\Chrome\User Data", new BrowserTypeLaunchPersistentContextOptions
            {
                Headless = false,
                Channel = "chrome",
            });*/
            _browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",
                Args = new[] 
                { 
                    "--start-maximized"
                }
            });

            _context = await _browser.NewContextAsync();
        }
    }

    public void SetPage(IPage page) { _page = page; }

    public void Dispose()
    {
        _playwright.Dispose();
    }
}
