namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ChangeCheckbox(BrowserActionParams actionParams)
    {
        var result = new BrowserActionResult();

        await _instance.Wait(actionParams.ContextId);

        // Retrieve the page raw html and infer the element path
        var regexExpression = actionParams.Context.MatchRule.ToLower() switch
        {
            "startwith" => $"^{actionParams.Context.ElementText}",
            "endwith" => $"{actionParams.Context.ElementText}$",
            "contains" => $"{actionParams.Context.ElementText}",
            _ => $"^{actionParams.Context.ElementText}$"
        };
        var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
        var elements = _instance.GetPage(actionParams.ContextId).GetByText(regex);
        var count = await elements.CountAsync();

        var errorMessage = $"Can't locate element by keyword {actionParams.Context.ElementText}";
        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }
        else if (count > 1)
        {
            result.Message = $"Located multiple elements by {actionParams.Context.ElementText}";
            _logger.LogError(result.Message);
            var allElements = await elements.AllAsync();
            foreach (var element in allElements)
            {

            }
            return result;
        }

        var parentElement = elements.Locator("..");
        count = await parentElement.CountAsync();
        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }

        var id = await elements.GetAttributeAsync("for");
        if (id == null)
        {
            elements = parentElement.Locator("input");
        }
        else
        {
            elements = _instance.GetPage(actionParams.ContextId).Locator($"#{id}");
        }
        count = await elements.CountAsync();

        if (count == 0)
        {
            result.Message = errorMessage;
            return result;
        }
        else if (count > 1)
        {
            result.Message = $"Located multiple elements by {actionParams.Context.ElementText}";
            _logger.LogError(result.Message);
            return result;
        }

        try
        {
            var isChecked = await elements.IsCheckedAsync();
            if (actionParams.Context.UpdateValue == "check" && !isChecked)
            {
                await elements.ClickAsync();
            }
            else if (actionParams.Context.UpdateValue == "uncheck" && isChecked)
            {
                await elements.ClickAsync();
            }

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
