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
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        await _browser.LaunchBrowser(args.Url);
        message.Content = string.IsNullOrEmpty(args.Url) ? $"Launch browser with blank page successfully." : $"Open website {args.Url} successfully.";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
