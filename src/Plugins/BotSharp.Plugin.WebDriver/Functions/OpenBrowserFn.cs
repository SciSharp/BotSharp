namespace BotSharp.Plugin.WebDriver.Functions;

public class OpenBrowserFn : IFunctionCallback
{
    public string Name => "open_browser";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public OpenBrowserFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var url = webDriverService.ReplaceToken(args.Url);

        url = url.Replace("https://https://", "https://");
        var result = await _browser.LaunchBrowser(convService.ConversationId, url);

        if (result)
        {
            message.Content = string.IsNullOrEmpty(url) ? $"Launch browser with blank page successfully." : $"Open website {url} successfully.";
        }
        else
        {
            message.Content = "Launch browser failed.";
        }

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(convService.ConversationId, path);

        return result;
    }
}
