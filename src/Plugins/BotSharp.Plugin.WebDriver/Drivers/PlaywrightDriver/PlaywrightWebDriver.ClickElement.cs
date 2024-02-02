using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ClickElement(Agent agent, BrowsingContextIn context, string messageId)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);

        // Retrieve the page raw html and infer the element path
        var regex = new Regex($"{context.InputText}$", RegexOptions.IgnoreCase);
        var elements = _instance.Page.GetByText(regex);
        var count = await elements.CountAsync();

        // try placeholder
        if (count == 0)
        {
            elements = _instance.Page.GetByPlaceholder(regex);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            throw new Exception($"Can't locate element by keyword {context.InputText}");
        }
        else if (count > 1)
        {
            _logger.LogWarning($"Multiple elements are found by keyword {context.InputText}");
        }

        await elements.ClickAsync();
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }
}
