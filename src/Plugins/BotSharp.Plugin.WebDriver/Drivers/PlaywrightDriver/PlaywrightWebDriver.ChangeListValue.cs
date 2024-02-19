using Microsoft.Extensions.Logging;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ChangeListValue(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        // Retrieve the page raw html and infer the element path
        var body = await _instance.GetPage(actionParams.ConversationId).QuerySelectorAsync("body");

        var str = new List<string>();
        var inputs = await body.QuerySelectorAllAsync("select");
        foreach (var input in inputs)
        {
            var html = "<select";
            var id = await input.GetAttributeAsync("id");
            if (!string.IsNullOrEmpty(id))
            {
                html += $" id='{id}'";
            }
            var name = await input.GetAttributeAsync("name");
            if (!string.IsNullOrEmpty(name))
            {
                html += $" name='{id}'";
            }
            html += ">";

            var options = await input.QuerySelectorAllAsync("option");
            if (options != null)
            {
                foreach (var option in options)
                {
                    html += "<option";
                    var value = await option.GetAttributeAsync("value");
                    if (!string.IsNullOrEmpty(value))
                    {
                        html += $" value='{value}'";
                    }
                    html += ">";
                    var text = await option.TextContentAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        html += text;
                    }
                    else
                    {
                        html += "'<NULL>'";
                    }
                    html += "</option>";
                }
            }

            html += "</select>";
            str.Add(html);
        }

        var driverService = _services.GetRequiredService<WebDriverService>();
        var htmlElementContextOut = await driverService.InferElement(actionParams.Agent, 
            string.Join("", str),
            actionParams.Context.ElementName,
            actionParams.MessageId);
        ILocator element = Locator(actionParams.ConversationId, htmlElementContextOut);
        
        try
        {
            var isVisible = await element.IsVisibleAsync();

            if (!isVisible)
            {
                // Select the element you want to make visible (replace with your own selector)
                var control = await _instance.GetPage(actionParams.ConversationId)
                    .QuerySelectorAsync($"#{htmlElementContextOut.ElementId}");

                // Show the element by modifying its CSS styles
                await _instance.GetPage(actionParams.ConversationId)
                    .EvaluateAsync(@"(element) => {
                        element.style.display = 'block';
                        element.style.visibility = 'visible';
                    }", control);
            }

            await element.FocusAsync();
            await element.SelectOptionAsync(new SelectOptionValue
            {
                Label = actionParams.Context.UpdateValue
            });

            // Click on the blank area to activate posting
            // await body.ClickAsync();
            if (!isVisible)
            {
                // Select the element you want to make visible (replace with your own selector)
                var control = await _instance.GetPage(actionParams.ConversationId)
                    .QuerySelectorAsync($"#{htmlElementContextOut.ElementId}");

                // Show the element by modifying its CSS styles
                await _instance.GetPage(actionParams.ConversationId).EvaluateAsync(@"(element) => {
                    element.style.display = 'none';
                    element.style.visibility = 'hidden';
                }", control);
            }
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return false;
    }
}
