namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ClickButton(Agent agent, BrowsingContextIn context, string messageId)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Find by text exactly match
        var elements = _instance.Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            Name = context.ElementName
        });

        if (await elements.CountAsync() == 0)
        {
            // Infer element if not found
            var driverService = _services.GetRequiredService<WebDriverService>();
            var html = await FilteredButtonHtml();
            var htmlElementContextOut = await driverService.InferElement(agent,
                html,
                context.ElementName,
                messageId);
            elements = Locator(htmlElementContextOut);
        }

        await elements.ClickAsync();
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    private async Task<string> FilteredButtonHtml()
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

        return string.Join("", str);
    }
}
