namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebCloseBrowserFn : IFunctionCallback
{
    public string Name => "util-web-close_browser";
    public string Indication => "Closing web browser.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebCloseBrowserFn(
        IServiceProvider services,
        ILogger<UtilWebCloseBrowserFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var conv = _services.GetRequiredService<IConversationService>();
        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var services = _services.CreateScope().ServiceProvider;
        var browser = services.GetRequiredService<IWebBrowser>();
        var msg = new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            ContextId = webDriverService.GetMessageContext(message)
        };

        await browser.CloseBrowser(message.CurrentAgentId);

        message.Content = $"Browser closed.";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await browser.ScreenshotAsync(msg, path);

        return true;
    }
}
