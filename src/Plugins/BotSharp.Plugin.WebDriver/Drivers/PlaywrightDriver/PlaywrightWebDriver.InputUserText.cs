namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task InputUserText(Agent agent, BrowsingContextIn context, string messageId)
    {
        var driverService = _services.GetRequiredService<WebDriverService>();

        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        var inputs = await body.QuerySelectorAllAsync("input");
        foreach (var input in inputs)
        {
            var text = await input.TextContentAsync();
            var id = await input.GetAttributeAsync("id");
            var name = await input.GetAttributeAsync("name");
            var type = await input.GetAttributeAsync("type");
            str.Add(driverService.AssembleMarkup("input", new MarkupProperties
            {
                Id = id,
                Name = name,
                Type = type,
                Text = text
            }));
        }

        inputs = await body.QuerySelectorAllAsync("textarea");
        foreach (var input in inputs)
        {
            var text = await input.TextContentAsync();
            var id = await input.GetAttributeAsync("id");
            var name = await input.GetAttributeAsync("name");
            var type = await input.GetAttributeAsync("type");
            str.Add(driverService.AssembleMarkup("textarea", new MarkupProperties
            {
                Id = id,
                Name = name,
                Type = type,
                Text = text
            }));
        }
        
        var htmlElementContextOut = await driverService.LocateElement(agent, 
            string.Join("", str), 
            context.ElementName, 
            messageId);
        ILocator element = Locator(htmlElementContextOut);

        try
        {
            await element.FillAsync(context.InputText);
            if (context.PressEnter)
            {
                await element.PressAsync("Enter");
            }
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
