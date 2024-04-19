namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<string> GetAttributeValue(MessageInfo message, ElementLocatingArgs location, BrowserActionResult result)
    {
        var driver = await _instance.InitInstance(message.ContextId);
        var locator = driver.FindElement(By.CssSelector(result.Selector));
        var value = string.Empty;

        if (!string.IsNullOrEmpty(location?.AttributeName))
        {
            value = locator.GetAttribute(location.AttributeName);
        }

        return value ?? string.Empty;
    }
}
