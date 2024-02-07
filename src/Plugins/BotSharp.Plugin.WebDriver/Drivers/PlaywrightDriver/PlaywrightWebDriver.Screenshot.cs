
namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<string> ScreenshotAsync(string path)
    {
        var bytes = await _instance.Page.ScreenshotAsync(new PageScreenshotOptions
        {
            Path = path,
        });

        return "data:image/png;base64," + Convert.ToBase64String(bytes);
    }
}
