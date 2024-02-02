using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class ChangeListValueFn : IFunctionCallback
{
    public string Name => "change_list_value";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public ChangeListValueFn(IServiceProvider services,
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
        await _driver.ChangeListValue(agent, args, message.MessageId);

        message.Content = $"Updat the value of \"{args.ElementName}\" to \"{args.UpdateValue}\" successfully.";
        return true;
    }
}
