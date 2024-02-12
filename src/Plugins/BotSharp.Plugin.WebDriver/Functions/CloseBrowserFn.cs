namespace BotSharp.Plugin.WebDriver.Functions;

public class CloseBrowserFn : IFunctionCallback
{
    public string Name => "close_browser";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public CloseBrowserFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        await _browser.CloseBrowser(convService.ConversationId);
        message.Content = $"Browser is closed {convService.ConversationId}";
        return true;
    }
}
