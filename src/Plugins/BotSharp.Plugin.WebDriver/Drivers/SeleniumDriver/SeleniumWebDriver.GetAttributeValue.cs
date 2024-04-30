namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> GetAttributeValue(MessageInfo message, ElementLocatingArgs location)
    {
        var driver = await _instance.InitInstance(message.ContextId);
        var locator = driver.FindElement(By.CssSelector(location.Selector));
        var value = string.Empty;

        if (!string.IsNullOrEmpty(location?.AttributeName))
        {
            value = locator.GetAttribute(location.AttributeName);
        }

        return new BrowserActionResult 
        {
            IsSuccess = true,
            Body = value ?? string.Empty 
        };
    }
}
