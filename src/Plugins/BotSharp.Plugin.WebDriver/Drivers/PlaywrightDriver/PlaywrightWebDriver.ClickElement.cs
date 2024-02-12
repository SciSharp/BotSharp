using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ClickElement(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        // Retrieve the page raw html and infer the element path
        var regexExpression = actionParams.Context.MatchRule.ToLower() switch
        {
            "startwith" => $"^{actionParams.Context.ElementText}",
            "endwith" => $"{actionParams.Context.ElementText}$",
            "contains" => $"{actionParams.Context.ElementText}",
            _ => $"^{actionParams.Context.ElementText}$"
        };
        var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
        var elements = _instance.GetPage(actionParams.ConversationId).GetByText(regex);
        var count = await elements.CountAsync();

        // try placeholder
        if (count == 0)
        {
            elements = _instance.GetPage(actionParams.ConversationId).GetByPlaceholder(regex);
            count = await elements.CountAsync();
        }

        if (count == 0)
        {
            _logger.LogError($"Can't locate element by keyword {actionParams.Context.ElementText}");
        }
        else if (count == 1)
        {
            // var tagName = await elements.EvaluateAsync<string>("el => el.tagName");

            await elements.ClickAsync();

            // Triggered ajax
            await _instance.Wait(actionParams.ConversationId);

            return true;
        }
        else if (count > 1)
        {
            _logger.LogWarning($"Multiple elements are found by keyword {actionParams.Context.ElementText}");
            var all = await elements.AllAsync();
            foreach (var element in all)
            {
                var content = await element.TextContentAsync();
                _logger.LogWarning(content);
            }
        }

        return false;
    }
}
