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

    public async Task<BrowserActionResult> SetAttributeValue(MessageInfo message, ElementLocatingArgs location)
    {
        var page = _instance.GetPage(message.ContextId);
        ILocator locator = page.Locator(location.Selector);
        var elementCount = await locator.CountAsync();

        if (elementCount > 0)
        {
            foreach (var element in await locator.AllAsync())
            {
                var script = $"element => element.{location.AttributeName} = '{location.AttributeValue}'";
                var result = await locator.EvaluateAsync(script);
            }
        }

        return new BrowserActionResult
        {
            IsSuccess = true
        };
    }
}
