
namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> ScreenshotAsync(string conversationId, string path)
    {
        await _instance.Wait(conversationId);
        var page = _instance.GetPage(conversationId);

        await Task.Delay(500);
        var bytes = await page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path
        });

        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
