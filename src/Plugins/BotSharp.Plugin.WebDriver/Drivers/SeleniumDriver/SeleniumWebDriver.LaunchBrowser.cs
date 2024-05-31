namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> LaunchBrowser(MessageInfo message, string? url)
    {
        var result = new BrowserActionResult()
        {
            IsSuccess = true
        };
        var context = await _instance.InitInstance(message.ContextId);

        if (!string.IsNullOrEmpty(url))
        {
            // Check if the page is already open
            var page = await _instance.NewPage(message.ContextId);

            try
            {
                page.GoToUrl(url);
                result.IsSuccess = true;
            }
            catch (Exception ex)
            {
                result.Message = ex.Message;
                result.StackTrace = ex.StackTrace;
                _logger.LogError(ex.Message);
            }
        }

        return result;
    }
}
