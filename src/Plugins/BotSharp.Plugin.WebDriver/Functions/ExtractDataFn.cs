
using BotSharp.Abstraction.Agents;
using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class ExtractDataFn : IFunctionCallback
{
    public string Name => "extract_data_from_page";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public ExtractDataFn(IServiceProvider services,
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
        message.Content = await _driver.ExtractData(agent, args, message.MessageId);
        return true;
    }
}
