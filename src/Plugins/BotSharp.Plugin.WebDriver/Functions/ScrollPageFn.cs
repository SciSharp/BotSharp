namespace BotSharp.Plugin.WebDriver.Functions;

public class ScrollPageFn : IFunctionCallback
{
    public string Name => "scroll_page";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ScrollPageFn(IServiceProvider services,
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
        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        message.Data = await _browser.ScrollPage(new MessageInfo
        {
            AgentId = agent.Id,
            ContextId = webDriverService.GetMessageContext(message),
            MessageId = message.MessageId
        }, new PageActionArgs
        {
            Action = BroswerActionEnum.Scroll,
            Direction = args.Direction,
        });
    
        message.Content = "Scrolled. You can scroll more if needed.";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = webDriverService.GetMessageContext(message),
            MessageId = message.MessageId
        }, path);

        return true;
    }
}
