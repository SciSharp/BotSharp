namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<IBrowser> LaunchBrowser(string? url)
    {
        if (_instance.Playwright == null)
        {
            var playwright = await Playwright.CreateAsync();
            _instance.SetPlaywright(playwright);
        }

        if (_instance.Browser == null)
        {
            var browser = await _instance.Playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
            {
                Headless = false,
                Channel = "chrome",
            });
            _instance.SetBrowser(browser);
        }

        if (!string.IsNullOrEmpty(url))
        {
            var page = await _instance.Browser.NewPageAsync();
            _instance.SetPage(page);
            var response = await page.GotoAsync(url);
        }

        return _instance.Browser;
    }
}
