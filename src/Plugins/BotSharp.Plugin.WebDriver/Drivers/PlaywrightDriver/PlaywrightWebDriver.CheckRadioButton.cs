using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> CheckRadioButton(BrowserActionParams actionParams)
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
        
        if (count == 0)
        {
            return false;
        }

        var parentElement = elements.Locator("..");
        count = await parentElement.CountAsync();
        if (count == 0)
        {
            return false;
        }

        elements = parentElement.GetByText(new Regex($"{actionParams.Context.UpdateValue}", RegexOptions.IgnoreCase));

        count = await elements.CountAsync();

        if (count == 0)
        {
            return false;
        }

        try
        {
            // var label = await elements.GetAttributeAsync("for");
            await elements.SetCheckedAsync(true);

            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
        }

        return false;
    }
}
