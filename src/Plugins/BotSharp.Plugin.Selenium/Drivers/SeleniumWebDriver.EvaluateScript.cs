namespace BotSharp.Plugin.Selenium.Drivers;

public partial class SeleniumWebDriver
{
    public async Task<T> EvaluateScript<T>(string contextId, string script)
    {
        await _instance.Wait(contextId);
        var driver = await _instance.InitContext(contextId);
        var jsExecutor = (IJavaScriptExecutor)driver;
        var result = jsExecutor.ExecuteAsyncScript(script);
        return (T)result;
    }
}
