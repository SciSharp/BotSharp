namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebTakeScreenshotFn : IFunctionCallback
{
    public string Name => "util-web-take_screenshot";
    public string Indication => "Taking screenshot for current viewport.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebTakeScreenshotFn(
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

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);
        message.Content = "Took screenshot completed. You can take another screenshot if needed.";

        var screenshot = await browser.ScreenshotAsync(msg, path);
        message.Data = screenshot.Body;

        return true;
    }
}
