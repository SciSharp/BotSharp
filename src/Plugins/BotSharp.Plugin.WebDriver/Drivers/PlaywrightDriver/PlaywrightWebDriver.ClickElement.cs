using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task ClickElement(Agent agent, BrowsingContextIn context, string messageId)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Retrieve the page raw html and infer the element path
        var regexExpression = context.MatchRule.ToLower() switch
        {
            "startwith" => $"^{context.ElementText}",
            "endwith" => $"{context.ElementText}$",
            "contains" => $"{context.ElementText}",
            _ => $"^{context.ElementText}$"
        };
        var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
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
            throw new Exception($"Can't locate element by keyword {context.ElementText}");
        }
        else if (count > 1)
        {
            _logger.LogWarning($"Multiple elements are found by keyword {context.ElementText}");
        }

        await elements.ClickAsync();
    }
}
