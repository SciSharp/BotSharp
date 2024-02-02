namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task SwitchToNewTab()
    {
        var page = _instance.Context.Pages.Last();
        _instance.SetPage(page);
        await page.BringToFrontAsync();
    }
}
