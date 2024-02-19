namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    public async Task<T> EvaluateScript<T>(string conversationId, string script)
    {
        await _instance.Wait(conversationId);

        return await _instance.GetPage(conversationId).EvaluateAsync<T>(script);
    }
}
