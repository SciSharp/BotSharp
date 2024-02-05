using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class InputUserTextFn : IFunctionCallback
{
    public string Name => "input_user_text";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public InputUserTextFn(IServiceProvider services,
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
        await _driver.InputUserText(agent, args, message.MessageId);

        message.Content = $"Input text \"{args.InputText}\"";
        if (args.PressEnter.HasValue && args.PressEnter.Value)
        {
            message.Content += " and pressed Enter";
        }
        message.Content += " successfully.\"";

        return true;
    }
}
