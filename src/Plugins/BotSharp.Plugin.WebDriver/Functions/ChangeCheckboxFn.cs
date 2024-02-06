namespace BotSharp.Plugin.WebDriver.Functions;

public class ChangeCheckboxFn : IFunctionCallback
{
    public string Name => "change_checkbox";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ChangeCheckboxFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        var result = await _browser.ChangeCheckbox(new BrowserActionParams(agent, args, message.MessageId));

        message.Content = result ? "Success" : "Failed";

        return true;
    }
}
