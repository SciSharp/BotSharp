
namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> ScrollPageAsync(BrowserActionParams actionParams)
    {
        await _instance.Wait(actionParams.ConversationId);

        var page = _instance.GetPage(actionParams.ConversationId);

        if(actionParams.Context.Direction == "down")
            await page.EvaluateAsync("window.scrollBy(0, window.innerHeight - 200)");
        else if (actionParams.Context.Direction == "up")
            await page.EvaluateAsync("window.scrollBy(0, -window.innerHeight + 200)");
        else if (actionParams.Context.Direction == "left")
            await page.EvaluateAsync("window.scrollBy(-400, 0)");
        else if (actionParams.Context.Direction == "right")
            await page.EvaluateAsync("window.scrollBy(400, 0)");

        return true;
    }
}
