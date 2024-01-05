using BotSharp.Plugin.WebDriver.Services;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ClickElement(Agent agent, BrowsingContextIn context, string messageId)
    {
        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        var anchors = await body.QuerySelectorAllAsync("a");
        foreach (var a in anchors)
        {
            var text = await a.TextContentAsync();
            str.Add($"<a>{(string.IsNullOrEmpty(text) ? "EMPTY" : text)}</a>");
        }

        var buttons = await body.QuerySelectorAllAsync("button");
        foreach (var btn in buttons)
        {
            var text = await btn.TextContentAsync();
            str.Add($"<button>{text}</button>");
        }

        var driverService = _services.GetRequiredService<WebDriverService>();
        var htmlElementContextOut = await driverService.FindElement(agent, string.Join("", str), context.ElementName, messageId);

        var element = _instance.Page.Locator(htmlElementContextOut.TagName).Nth(htmlElementContextOut.Index);
        await element.ClickAsync();
    }
}
