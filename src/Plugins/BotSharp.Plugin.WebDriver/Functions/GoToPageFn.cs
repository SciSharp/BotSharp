namespace BotSharp.Plugin.WebDriver.Functions;

public class GoToPageFn : IFunctionCallback
{
    public string Name => "go_to_page";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public GoToPageFn(IServiceProvider services,
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
        await _browser.GoToPage(new BrowserActionParams(agent, args, message.MessageId));
        message.Content = $"Page {args.Url} is open.";
        return true;
    }
}
