using BotSharp.Abstraction.Browsing.Settings;

namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebGoToPageFn : IFunctionCallback
{
    public string Name => "util-web-go_to_page";
    public string Indication => "Open web page.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebGoToPageFn(
        IServiceProvider services,
        ILogger<UtilWebGoToPageFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<PageActionArgs>(message.FunctionArgs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var _webDriver = _services.GetRequiredService<WebBrowsingSettings>();
        args.Timeout = _webDriver.DefaultTimeout;
        args.WaitForNetworkIdle = false;
        args.WaitTime = _webDriver.DefaultWaitTime;

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
        if (!args.KeepBrowserOpen)
        {
            await browser.CloseCurrentPage(msg);
        }
        var result = await browser.GoToPage(msg, args);
        if (!result.IsSuccess)
        {
            message.Content = "Open web page failed.";
            return false;
        }

        message.Content = $"Open web page successfully.";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await browser.ScreenshotAsync(msg, path);

        return true;
    }
}
