using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ClickElement(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        var page = _instance.GetPage(actionParams.ConversationId);
        ILocator locator = default;
        int count = 0;

        // Retrieve the page raw html and infer the element path
        if (!string.IsNullOrEmpty(actionParams.Context.ElementText))
        {
            var regexExpression = actionParams.Context.MatchRule.ToLower() switch
            {
                "startwith" => $"^{actionParams.Context.ElementText}",
                "endwith" => $"{actionParams.Context.ElementText}$",
                "contains" => $"{actionParams.Context.ElementText}",
                _ => $"^{actionParams.Context.ElementText}$"
            };
            var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
            locator = page.GetByText(regex);
            count = await locator.CountAsync();

            // try placeholder
            if (count == 0)
            {
                locator = page.GetByPlaceholder(regex);
                count = await locator.CountAsync();
            }
        }

        // try attribute
        if (count == 0 && !string.IsNullOrEmpty(actionParams.Context.AttributeName))
        {
            locator = page.Locator($"[{actionParams.Context.AttributeName}='{actionParams.Context.AttributeValue}']");
            count = await locator.CountAsync();
        }

        if (count == 0)
        {
            _logger.LogError($"Can't locate element by keyword {actionParams.Context.ElementText}");
        }
        else if (count == 1)
        {
            // var tagName = await elements.EvaluateAsync<string>("el => el.tagName");

            await locator.ClickAsync();

            // Triggered ajax
            await _instance.Wait(actionParams.ConversationId);

            return true;
        }
        else if (count > 1)
        {
            _logger.LogWarning($"Multiple elements are found by keyword {actionParams.Context.ElementText}");
            var all = await locator.AllAsync();
            foreach (var element in all)
            {
                var content = await element.TextContentAsync();
                _logger.LogWarning(content);
            }
        }

        return false;
    }
}
