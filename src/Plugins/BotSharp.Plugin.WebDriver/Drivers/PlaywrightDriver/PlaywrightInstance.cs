namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public class PlaywrightInstance : IDisposable
{
    IPlaywright _playwright;
    IBrowser _browser;
    IPage _page;

    public IPlaywright Playwright => _playwright;
    public IBrowser Browser => _browser;
    public IPage Page => _page;

    public void SetPlaywright(IPlaywright playwright) { _playwright = playwright; }
    public void SetBrowser(IBrowser browser) { _browser = browser; }
    public void SetPage(IPage page) { _page = page; }

    public void Dispose()
    {
        _playwright.Dispose();
    }
}
