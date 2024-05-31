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
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var url = webDriverService.ReplaceToken(args.Url);

        url = url.Replace("https://https://", "https://");

        var result = await _browser.GoToPage(new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = convService.ConversationId,
            MessageId = message.MessageId
        }, url);
        message.Content = result.IsSuccess ? $"Page {url} is open." : $"Page {url} open failed. {result.Message}";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = convService.ConversationId,
            MessageId = message.MessageId
        }, path);

        return result.IsSuccess;
    }
}
