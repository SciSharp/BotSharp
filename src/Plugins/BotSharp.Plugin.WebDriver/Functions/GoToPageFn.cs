using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class GoToPageFn : IFunctionCallback
{
    public string Name => "go_to_page";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public GoToPageFn(IServiceProvider services,
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
        await _driver.GoToPage(agent, args, message.MessageId);
        message.Content = $"Page {args.Url} is open.";
        return true;
    }
}
