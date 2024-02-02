using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class CloseBrowserFn : IFunctionCallback
{
    public string Name => "close_browser";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public CloseBrowserFn(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        await _driver.CloseBrowser(agent, args, message.MessageId);
        message.Content = $"Browser is closed";
        return true;
    }
}