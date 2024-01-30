namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ClickElement(Agent agent, BrowsingContextIn context, string messageId)
    {
        var driverService = _services.GetRequiredService<WebDriverService>();

        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        /*var anchors = await body.QuerySelectorAllAsync("a");
        foreach (var a in anchors)
        {
            var text = await a.TextContentAsync();
            str.Add($"<a>{(string.IsNullOrEmpty(text) ? "EMPTY" : text)}</a>");
        }*/

        var buttons = await body.QuerySelectorAllAsync("button");
        foreach (var btn in buttons)
        {
            var text = await btn.TextContentAsync();
            var name = await btn.GetAttributeAsync("name");
            var id = await btn.GetAttributeAsync("id");
            str.Add(driverService.AssembleMarkup("button", new MarkupProperties
            {
                Id = id,
                Name = name,
                Text = text
            }));
        }

        var htmlElementContextOut = await driverService.LocateElement(agent, 
            string.Join("", str), 
            context.ElementName, 
            messageId);
        ILocator element = Locator(htmlElementContextOut);
        await element.ClickAsync();
    }
}
