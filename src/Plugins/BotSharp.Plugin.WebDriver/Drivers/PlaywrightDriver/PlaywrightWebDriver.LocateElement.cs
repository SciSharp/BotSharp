namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> LocateElement(MessageInfo message, ElementLocatingArgs location)
    {
        var result = new BrowserActionResult();
        var page = _instance.GetPage(message.ConversationId);
        ILocator locator = page.Locator("body");
        int count = 0;

        // check if selector is specified
        if (location.Selector != null)
        {
            locator = page.Locator(location.Selector);
            count = await locator.CountAsync();
        }

        // try attribute
        if (count == 0 && !string.IsNullOrEmpty(location.AttributeName))
        {
            locator = locator.Locator($"[{location.AttributeName}='{location.AttributeValue}']");
            count = await locator.CountAsync();
        }

        // Retrieve the page raw html and infer the element path
        if (!string.IsNullOrEmpty(location.Text))
        {
            var regexExpression = location.MatchRule.ToLower() switch
            {
                "startwith" => $"^{location.Text}",
                "endwith" => $"{location.Text}$",
                "contains" => $"{location.Text}",
                _ => $"^{location.Text}$"
            };
            var regex = new Regex(regexExpression, RegexOptions.IgnoreCase);
            locator = locator.GetByText(regex);
            count = await locator.CountAsync();

            // try placeholder
            if (count == 0)
            {
                locator = locator.GetByPlaceholder(regex);
                count = await locator.CountAsync();
            }
        }

        if (location.Index >= 0)
        {
            locator = locator.Nth(location.Index);
            count = await locator.CountAsync();
        }

        if (count == 0)
        {
            result.ErrorMessage = $"Can't locate element by keyword {location.Text}";
            _logger.LogError(result.ErrorMessage);
        }
        else if (count == 1)
        {
            result.Selector = locator.ToString().Split('@').Last();
            var text = await locator.InnerTextAsync();
            result.Body = text;
            result.IsSuccess = true;
        }
        else if (count > 1)
        {
            if (location.FailIfMultiple)
            {
                result.ErrorMessage = $"Multiple elements are found by {locator}";
                _logger.LogError(result.ErrorMessage);

                foreach (var element in await locator.AllAsync())
                {
                    var content = await element.InnerHTMLAsync();
                    _logger.LogError(content);
                }
            }
            else
            {
                result.Selector = locator.ToString();
                result.IsSuccess = true;
            }
        }

        return result;
    }
}
