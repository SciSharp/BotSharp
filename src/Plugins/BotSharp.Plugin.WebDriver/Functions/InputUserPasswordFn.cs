using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class InputUserPasswordFn : IFunctionCallback
{
    public string Name => "input_user_password";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public InputUserPasswordFn(IServiceProvider services,
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
        await _driver.Instance.Page.WaitForLoadStateAsync(LoadState.Load);
        await _driver.InputUserPassword(agent, args, message.MessageId);

        message.Content = "Input password successfully";
        return true;
    }
}
