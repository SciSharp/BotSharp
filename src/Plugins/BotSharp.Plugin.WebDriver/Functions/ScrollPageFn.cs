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
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        message.Data = await _browser.ScrollPageAsync(new BrowserActionParams(agent, args, convService.ConversationId, message.MessageId));
        message.Content = "Scrolled";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(convService.ConversationId, path);

        return true;
    }
}
