using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> InputUserText(BrowserActionParams actionParams)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Find by text exactly match
        var elements = _instance.Page.GetByRole(AriaRole.Textbox, new PageGetByRoleOptions
        {
            Name = actionParams.Context.ElementText
        });
        var count = await elements.CountAsync();
        if (count == 0)
        {
            elements = _instance.Page.GetByPlaceholder(actionParams.Context.ElementText);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            var driverService = _services.GetRequiredService<WebDriverService>();
            var html = await FilteredInputHtml();
            var htmlElementContextOut = await driverService.InferElement(actionParams.Agent,
                html,
                actionParams.Context.ElementText,
                actionParams.MessageId);
            elements = Locator(htmlElementContextOut);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {

        }
        else if (count == 1)
        {
            try
            {
                await elements.FillAsync(actionParams.Context.InputText);
                if (actionParams.Context.PressEnter.HasValue && actionParams.Context.PressEnter.Value)
                {
                    await elements.PressAsync("Enter");
                }

                // Triggered ajax
                await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
            }
        }

        return false;
    }

    private async Task<string> FilteredInputHtml()
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
            var placeholder = await input.GetAttributeAsync("placeholder");

            str.Add(driverService.AssembleMarkup("input", new MarkupProperties
            {
                Id = id,
                Name = name,
                Type = type,
                Text = text,
                Placeholder = placeholder
            }));
        }

        inputs = await body.QuerySelectorAllAsync("textarea");
        foreach (var input in inputs)
        {
            var text = await input.TextContentAsync();
            var id = await input.GetAttributeAsync("id");
            var name = await input.GetAttributeAsync("name");
            var type = await input.GetAttributeAsync("type");
            var placeholder = await input.GetAttributeAsync("placeholder");
            str.Add(driverService.AssembleMarkup("textarea", new MarkupProperties
            {
                Id = id,
                Name = name,
                Type = type,
                Text = text,
                Placeholder = placeholder
            }));
        }

        return string.Join("", str);
    }
}
