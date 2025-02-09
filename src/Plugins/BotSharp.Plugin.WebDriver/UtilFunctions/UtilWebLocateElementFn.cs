namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebLocateElementFn : IFunctionCallback
{
    public string Name => "util-web-locate_element";
    public string Indication => "Locating element.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebLocateElementFn(
        IServiceProvider services,
        ILogger<UtilWebLocateElementFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var locatorArgs = JsonSerializer.Deserialize<ElementLocatingArgs>(message.FunctionArgs ?? "{}");
        var conv = _services.GetRequiredService<IConversationService>();
        locatorArgs.Highlight = true;

        var browser = _services.GetRequiredService<IWebBrowser>();
        var msg = new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            ContextId = message.CurrentAgentId,
        };
        var result = await browser.LocateElement(msg, locatorArgs);

        message.Content = $"Locating element {(result.IsSuccess ? "success" : "failed")}";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await browser.ScreenshotAsync(msg, path);

        return true;
    }
}
