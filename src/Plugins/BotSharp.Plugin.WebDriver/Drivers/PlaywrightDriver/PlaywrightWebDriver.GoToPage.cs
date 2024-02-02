namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task GoToPage(Agent agent, BrowsingContextIn context, string messageId)
    {
        await _instance.Page.GotoAsync(context.Url);
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
    }
}
