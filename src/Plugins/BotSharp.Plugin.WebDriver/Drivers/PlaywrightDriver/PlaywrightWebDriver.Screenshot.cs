namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<BrowserActionResult> ScreenshotAsync(MessageInfo message, string path)
    {
        var result = new BrowserActionResult();
        if (_webBrowsingSettings.IsEnableScreenshot)
        {
            await _instance.Wait(message.ContextId, waitNetworkIdle: false);
            var page = _instance.GetPage(message.ContextId);

            await Task.Delay(300);
            var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
            {
                Path = path,
                FullPage = true
            });
            result.Body = "data:image/png;base64," + Convert.ToBase64String(bytes);
        }
        result.IsSuccess = true;
        return result;
    }
}
