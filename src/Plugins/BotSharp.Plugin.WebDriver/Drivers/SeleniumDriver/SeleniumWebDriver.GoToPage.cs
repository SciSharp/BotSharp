namespace BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;

public partial class SeleniumWebDriver
{
    public async Task<BrowserActionResult> GoToPage(string contextId, string url, bool openNewTab = false)
    {
        var result = new BrowserActionResult();
        try
        {
            var page = openNewTab ? await _instance.NewPage(contextId) :
                _instance.GetPage(contextId);
            page.GoToUrl(url);
            await _instance.Wait(contextId);
            
            result.Body = _instance.GetPageContent(contextId);
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
