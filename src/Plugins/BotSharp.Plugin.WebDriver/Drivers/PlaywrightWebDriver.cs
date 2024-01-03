using BotSharp.Plugin.WebDriver.Services;

namespace BotSharp.Plugin.WebDriver.Drivers;

public class PlaywrightWebDriver
{
    private readonly IServiceProvider _services;
    private readonly PlaywrightInstance _instance;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance)
    {
        _services = services;
        _instance = instance;
    }

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

    public async Task ClickElement(Agent agent, BrowsingContextIn context, string messageId)
    {
        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        var anchors = await body.QuerySelectorAllAsync("a");
        foreach (var a in anchors)
        {
            var text = await a.TextContentAsync();
            if (!string.IsNullOrEmpty(text))
            {
                str.Add($"<a>{text}</a>");
            }
        }
        
        var buttons = await body.QuerySelectorAllAsync("button");
        foreach (var btn in buttons)
        {
            var text = await btn.TextContentAsync();
            if (!string.IsNullOrEmpty(text))
            {
                str.Add($"<button>{text}</button>");
            }
        }

        var driverService = _services.GetRequiredService<WebDriverService>();
        var htmlElementContextOut = await driverService.FindElement(agent, string.Join("", str), context.ElementName, messageId);

        var element = _instance.Page.Locator(htmlElementContextOut.TagName).Nth(htmlElementContextOut.Index + 1);
        await element.ClickAsync();
    }
}
