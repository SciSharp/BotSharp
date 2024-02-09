using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ClickButton(BrowserActionParams actionParams)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await Task.Delay(300);

        // Find by text exactly match
        var elements = _instance.Page.GetByRole(AriaRole.Button, new PageGetByRoleOptions
        {
            Name = actionParams.Context.ElementName
        });
        var count = await elements.CountAsync();

        if (count == 0)
        {
            elements = _instance.Page.GetByRole(AriaRole.Link, new PageGetByRoleOptions
            {
                Name = actionParams.Context.ElementName
            });
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            elements = _instance.Page.GetByText(actionParams.Context.ElementName);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            // Infer element if not found
            var driverService = _services.GetRequiredService<WebDriverService>();
            var html = await FilteredButtonHtml();
            var htmlElementContextOut = await driverService.InferElement(actionParams.Agent,
                html,
                actionParams.Context.ElementName,
                actionParams.MessageId);
            elements = Locator(htmlElementContextOut);

            if (elements == null)
            {
                return false;
            }
        }

        try
        {
            await elements.HoverAsync();
            await elements.ClickAsync();

            await Task.Delay(300);
            return true;
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex.Message);
        }
        return false;
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
