using System.Web;

namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    /// <summary>
    /// Using attributes or text to locate element and return the selector
    /// </summary>
    /// <param name="message"></param>
    /// <param name="location"></param>
    /// <returns></returns>
    public async Task<BrowserActionResult> LocateElement(MessageInfo message, ElementLocatingArgs location)
    {
        var result = new BrowserActionResult();
        var page = _instance.GetPage(message.ContextId);
        ILocator locator = page.Locator("body");
        int count = 0;

        // check if selector is specified
        if (location.Selector != null)
        {
            locator = locator.Locator(location.Selector);
            count = await locator.CountAsync();
        }

        if (location.Tag != null)
        {
            locator = page.Locator(location.Tag);
            count = await locator.CountAsync();
        }

        // try attribute
        if (!string.IsNullOrEmpty(location.AttributeName))
        {
            locator = locator.Locator($"[{location.AttributeName}='{location.AttributeValue}']");
            count = await locator.CountAsync();
        }

        // Retrieve the page raw html and infer the element path
        if (!string.IsNullOrEmpty(location.Text))
        {
            var text = location.Text.Replace("(", "\\(").Replace(")", "\\)");
            var regexExpression = location.MatchRule.ToLower() switch
            {
                "startwith" => $"^{text}",
                "endwith" => $"{text}$",
                "contains" => $"{text}",
                _ => $"^{text}$"
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
            if (location.IgnoreIfNotFound)
            {
                result.Message = $"Can't locate element by keyword {location.Text} and Ignored";
                _logger.LogWarning(result.Message);
            }
            else
            {
                result.Message = $"Can't locate element by keyword {location.Text}";
                _logger.LogError(result.Message);
            }

        }
        else if (count == 1)
        {
            if (location.Parent)
            {
                locator = locator.Locator("..");
            }

            result.Selector = locator.ToString().Split("Locator@").Last();

            // Make sure the element is visible
            /*if (!await locator.IsVisibleAsync())
            {
                await locator.EvaluateAsync("element => element.style.height = '15px'");
                await locator.EvaluateAsync("element => element.style.width = '15px'");
                await locator.EvaluateAsync("element => element.style.opacity = '1.0'");
            }*/

            var html = await locator.InnerHTMLAsync();
            // fix if html has &
            result.Body = HttpUtility.HtmlDecode(html);
            result.IsSuccess = true;
        }
        else if (count > 1)
        {
            // Make sure the element is visible
            foreach (var element in await locator.AllAsync())
            {
                if (!await element.IsVisibleAsync())
                {
                    await element.EvaluateAsync("element => element.style.height = '10px'");
                    await element.EvaluateAsync("element => element.style.width = '10px'");
                    await element.EvaluateAsync("element => element.style.opacity = '1.0'");
                }
            }

            if (location.FailIfMultiple)
            {
                result.Message = $"Multiple elements are found by {locator}";
                _logger.LogError(result.Message);

                foreach (var element in await locator.AllAsync())
                {
                    var content = await element.InnerHTMLAsync();
                    _logger.LogError(content);
                }
            }
            else
            {
                foreach (var element in await locator.AllAsync())
                {
                    var html = await element.InnerHTMLAsync();
                    _logger.LogWarning(html);
                    // fix if html has &
                    result.Body = HttpUtility.HtmlDecode(html);
                    break;
                }

                result.IsSuccess = true;
            }
        }

        // Hightlight the element
        if (result.IsSuccess && location.Highlight)
        {
            var handle = await page.QuerySelectorAsync(result.Selector);

            await page.EvaluateAsync($@"
                (element) => {{
                    element.style.outline = '2px solid {location.HighlightColor}';
                }}", handle);

            result.IsHighlighted = true;
        }

        return result;
    }
}
