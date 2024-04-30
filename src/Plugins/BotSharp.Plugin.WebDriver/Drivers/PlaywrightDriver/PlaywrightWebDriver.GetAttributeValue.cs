namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> GetAttributeValue(MessageInfo message, ElementLocatingArgs location)
    {
        var page = _instance.GetPage(message.ContextId);
        ILocator locator = page.Locator(location.Selector);
        var value = string.Empty;

        if (!string.IsNullOrEmpty(location?.AttributeName))
        {
            value = await locator.GetAttributeAsync(location.AttributeName);
        }

        return new BrowserActionResult
        {
            IsSuccess = true,
            Body = value ?? string.Empty
        };
    }
}
