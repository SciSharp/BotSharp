using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ClickButton(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        // Find by text exactly match
        var elements = _instance.GetPage(actionParams.ConversationId)
            .GetByRole(AriaRole.Button, new PageGetByRoleOptions
            {
                Name = actionParams.Context.ElementName
            });
        var count = await elements.CountAsync();

        if (count == 0)
        {
            elements = _instance.GetPage(actionParams.ConversationId)
                .GetByRole(AriaRole.Link, new PageGetByRoleOptions
                {
                    Name = actionParams.Context.ElementName
                });
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            elements = _instance.GetPage(actionParams.ConversationId)
                .GetByText(actionParams.Context.ElementName);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            // Infer element if not found
            var driverService = _services.GetRequiredService<WebDriverService>();
            var html = await FilteredButtonHtml(actionParams.ConversationId);
            var htmlElementContextOut = await driverService.InferElement(actionParams.Agent,
                html,
                actionParams.Context.ElementName,
                actionParams.MessageId);
            elements = Locator(actionParams.ConversationId, htmlElementContextOut);

            if (elements == null)
            {
                return false;
            }
        }

        try
        {
            await elements.ClickAsync();
            await _instance.Wait(actionParams.ConversationId);

            return true;
        }
        catch (Exception ex) 
        {
            _logger.LogError(ex.Message);
        }
        return false;
    }

    private async Task<string> FilteredButtonHtml(string conversationId)
    {
        var driverService = _services.GetRequiredService<WebDriverService>();

        // Retrieve the page raw html and infer the element path
        var body = await _instance.GetPage(conversationId).QuerySelectorAsync("body");

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
