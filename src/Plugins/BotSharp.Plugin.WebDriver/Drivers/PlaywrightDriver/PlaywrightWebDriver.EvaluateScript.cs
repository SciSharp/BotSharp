namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<T> EvaluateScript<T>(string script)
    {
        await _instance.Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await _instance.Page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        return await _instance.Page.EvaluateAsync<T>(script);
    }
}
