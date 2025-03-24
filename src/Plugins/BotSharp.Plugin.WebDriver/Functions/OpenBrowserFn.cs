using BotSharp.Abstraction.Browsing.Settings;

namespace BotSharp.Plugin.WebDriver.Functions;

public class OpenBrowserFn : IFunctionCallback
{
    public string Name => "open_browser";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;
    private readonly WebBrowsingSettings _webBrowsingSettings;

    public OpenBrowserFn(IServiceProvider services,
        IWebBrowser browser,
        WebBrowsingSettings webBrowsingSettings)
    {
        _services = services;
        _browser = browser;
        _webBrowsingSettings = webBrowsingSettings;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var url = webDriverService.ReplaceToken(args.Url);
        var _webDriver = _services.GetRequiredService<WebBrowsingSettings>();
        url = url.Replace("https://https://", "https://");
        var msgInfo = new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = webDriverService.GetMessageContext(message),
            MessageId = message.MessageId
        };
        var result = await _browser.LaunchBrowser(msgInfo, new BrowserActionArgs
        {
            Headless = _webBrowsingSettings.Headless
        });
        result = await _browser.GoToPage(msgInfo, new PageActionArgs
        {
            Url = url,
            Timeout = _webDriver.DefaultTimeout
        });

        if (result.IsSuccess)
        {
            message.Content = string.IsNullOrEmpty(url) ? $"Launch browser with blank page successfully." : $"Open website {url} successfully.";
        }
        else
        {
            message.Content = $"Launch browser failed. {result.Message}";
        }

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = webDriverService.GetMessageContext(message),
            MessageId = message.MessageId
        }, path);

        return result.IsSuccess;
    }
}
