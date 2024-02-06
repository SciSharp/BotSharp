using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ChangeCheckbox(BrowserActionParams actionParams)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        // Retrieve the page raw html and infer the element path
        var regexExpression = actionParams.Context.MatchRule.ToLower() switch
        {
            "startwith" => $"^{actionParams.Context.ElementText}",
            "endwith" => $"{actionParams.Context.ElementText}$",
            "contains" => $"{actionParams.Context.ElementText}",
            _ => $"^{actionParams.Context.ElementText}$"
        };
        var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
        var elements = _instance.Page.GetByText(regex);
        var count = await elements.CountAsync();

        if (count == 0)
        {
            return false;
        }
        else if (count > 1)
        {
            _logger.LogError($"Located multiple elements by {actionParams.Context.ElementText}");
            var allElements = await elements.AllAsync();
            foreach (var element in allElements)
            {

            }
            return false;
        }

        var parentElement = elements.Locator("..");
        count = await parentElement.CountAsync();
        if (count == 0)
        {
            return false;
        }

        var id = await elements.GetAttributeAsync("for");
        if (id == null)
        {
            elements = parentElement.Locator("input");
        }
        else
        {
            elements = _instance.Page.Locator($"#{id}");
        }
        count = await elements.CountAsync();

        if (count == 0)
        {
            return false;
        }
        else if (count > 1)
        {
            _logger.LogError($"Located multiple elements by {actionParams.Context.ElementText}");
            return false;
        }

        try
        {
            var isChecked = await elements.IsCheckedAsync();
            if (actionParams.Context.UpdateValue == "checked" && !isChecked)
            {
                await elements.ClickAsync();
            }
            else if (actionParams.Context.UpdateValue == "unchecked" && isChecked)
            {
                await elements.ClickAsync();
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
