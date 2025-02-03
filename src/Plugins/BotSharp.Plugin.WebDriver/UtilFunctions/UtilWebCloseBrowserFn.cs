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

        var browser = _services.GetRequiredService<IWebBrowser>();
        await browser.CloseBrowser(message.CurrentAgentId);

        message.Content = $"Browser closed.";

        return true;
    }
}
