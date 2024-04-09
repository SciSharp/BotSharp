namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> GetAttributeValue(MessageInfo message, ElementLocatingArgs location, BrowserActionResult result)
    {
        var page = _instance.GetPage(message.ContextId);
        ILocator locator = page.Locator(result.Selector);
        var value = string.Empty;

        if (!string.IsNullOrEmpty(location?.AttributeName))
        {
            value = await locator.GetAttributeAsync(location.AttributeName);
        }

        return value ?? string.Empty;
    }
}
