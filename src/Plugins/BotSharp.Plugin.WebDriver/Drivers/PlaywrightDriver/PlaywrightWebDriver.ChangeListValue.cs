using BotSharp.Plugin.WebDriver.Services;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ChangeListValue(Agent agent, BrowsingContextIn context, string messageId)
    {
        // Retrieve the page raw html and infer the element path
        var body = await _instance.Page.QuerySelectorAsync("body");

        var str = new List<string>();
        var inputs = await body.QuerySelectorAllAsync("input");
        foreach (var input in inputs)
        {
            var text = await input.TextContentAsync();
            var name = await input.GetAttributeAsync("name");
            var type = await input.GetAttributeAsync("type");
            str.Add($"<input name='{name}' type='{type}'>{text}</input>");
        }

        inputs = await body.QuerySelectorAllAsync("textarea");
        foreach (var input in inputs)
        {
            var text = await input.TextContentAsync();
            var name = await input.GetAttributeAsync("name");
            var type = await input.GetAttributeAsync("type");
            str.Add($"<textarea name='{name}' type='{type}'>{text}</textarea>");
        }

        var driverService = _services.GetRequiredService<WebDriverService>();
        var htmlElementContextOut = await driverService.LocateElement(agent, string.Join("", str), context.ElementName, messageId);

        if (htmlElementContextOut.Index < 0)
        {
            throw new Exception($"Can't locate the web element {context.ElementName}.");
        }

        var element = _instance.Page.Locator(htmlElementContextOut.TagName).Nth(htmlElementContextOut.Index);
        try
        {
            await element.FillAsync(context.InputText);
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
        }
    }
}
