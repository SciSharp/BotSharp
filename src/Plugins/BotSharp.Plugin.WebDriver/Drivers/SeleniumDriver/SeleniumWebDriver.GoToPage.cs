namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> GoToPage(MessageInfo message, string url, bool openNewTab = false)
    {
        var result = new BrowserActionResult();
        try
        {
            var page = openNewTab ? await _instance.NewPage(message.ContextId) :
                _instance.GetPage(message.ContextId);
            page.GoToUrl(url);
            await _instance.Wait(message.ContextId);
            
            result.Body = _instance.GetPageContent(message.ContextId);
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
