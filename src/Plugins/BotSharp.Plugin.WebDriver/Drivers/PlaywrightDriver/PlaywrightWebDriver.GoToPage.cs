namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<bool> GoToPage(BrowserActionParams actionParams)
    {
        await _instance.Page.GotoAsync(actionParams.Context.Url);
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        return true;
    }
}
