namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<T> EvaluateScript<T>(string contextId, string script)
    {
        await _instance.Wait(contextId);

        return await _instance.GetPage(contextId).EvaluateAsync<T>(script);
    }
}
