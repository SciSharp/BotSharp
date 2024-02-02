namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<IBrowser> LaunchBrowser(string? url)
    {
        await _instance.InitInstance();

        if (!string.IsNullOrEmpty(url))
        {
            /*var page = await _instance.Browser.NewPageAsync(new BrowserNewPageOptions
            {
                ViewportSize = ViewportSize.NoViewport
            });*/
            var page = await _instance.Context.NewPageAsync();
            _instance.SetPage(page);
            var response = await page.GotoAsync(url);
            await page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        }

        return _instance.Browser;
    }
}
