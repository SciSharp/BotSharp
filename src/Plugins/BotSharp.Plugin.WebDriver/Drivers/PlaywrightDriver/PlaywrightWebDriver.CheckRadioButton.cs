namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> CheckRadioButton(BrowserActionParams actionParams)
    {
        var result = new BrowserActionResult();
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

        var errorMessage = $"Can't locate element by keyword {actionParams.Context.ElementText}";
        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }

        var parentElement = elements.Locator("..");
        count = await parentElement.CountAsync();
        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }

        elements = parentElement.GetByText(new Regex($"{actionParams.Context.UpdateValue}", RegexOptions.IgnoreCase));

        count = await elements.CountAsync();

        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }

        try
        {
            // var label = await elements.GetAttributeAsync("for");
            await elements.SetCheckedAsync(true);

            result.IsSuccess = true;
        }
        catch (Exception ex)
        {
            result.Message = ex.Message;
            result.StackTrace = ex.StackTrace;
            _logger.LogError(ex.Message);
        }

        return result;
    }
}
