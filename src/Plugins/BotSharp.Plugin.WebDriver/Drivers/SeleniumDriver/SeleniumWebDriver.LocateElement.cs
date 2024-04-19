using OpenQA.Selenium;
using System.Collections.ObjectModel;

namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
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
        var driver = await _instance.InitInstance(message.ContextId);

        IWebElement locator = driver.FindElement(By.TagName("body"));
        ReadOnlyCollection<IWebElement> elements = default;
        string selector = string.Empty;
        int count = 0;

        // check if selector is specified
        if (location.Selector != null)
        {
            selector = location.Selector;
            elements = driver.FindElements(By.CssSelector(location.Selector));
            count = elements.Count;
        }

        // try attribute
        if (count == 0 && !string.IsNullOrEmpty(location.AttributeName))
        {
            selector = $"[{location.AttributeName}='{location.AttributeValue}']";
            elements = driver.FindElements(By.CssSelector(selector));
            count = elements.Count;
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

            selector = $"//*[text() = '{location.Text}']";
            elements = driver.FindElements(By.XPath(selector));
            count = elements.Count;

            // try placeholder
            if (count == 0)
            {
                selector = $"[placeholder='{location.Text}']";
                elements = driver.FindElements(By.CssSelector(selector));
                count = elements.Count;
            }
        }

        if (location.Index >= 0)
        {
            locator = elements[location.Index];
            count = 1;
        }

        if (count == 0)
        {
            result.Message = $"Can't locate element by keyword {location.Text}";
            _logger.LogError(result.Message);
        }
        else if (count == 1)
        {
            locator = elements[0];
            result.Selector = selector;
            var text = locator.Text;
            result.Body = text;
            result.IsSuccess = true;
        }
        else if (count > 1)
        {
            if (location.FailIfMultiple)
            {
                result.Message = $"Multiple elements are found by {locator}";
                _logger.LogError(result.Message);

                /*foreach (var element in await locator.AllAsync())
                {
                    var content = await element.InnerHTMLAsync();
                    _logger.LogError(content);
                }*/
            }
            else
            {
                result.Selector = locator.ToString();
                result.IsSuccess = true;
            }
        }

        // Hightlight the element
        if (result.IsSuccess && location.Highlight)
        {
            /*var handle = await page.QuerySelectorAsync(result.Selector);

            await page.EvaluateAsync($@"
                (element) => {{
                    element.style.outline = '2px solid red';
                }}", handle);

            result.IsHighlighted = true;*/
        }

        return result;
    }
}
