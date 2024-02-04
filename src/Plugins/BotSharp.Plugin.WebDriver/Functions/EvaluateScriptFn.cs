using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class EvaluateScriptFn : IFunctionCallback
{
    public string Name => "evaluate_script";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public EvaluateScriptFn(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        message.Data = await _driver.EvaluateScript<object>(message.Content);
        return true;
    }
}
