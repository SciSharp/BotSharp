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
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var url = webDriverService.ReplaceToken(args.Url);

        url = url.Replace("https://https://", "https://");

        var result = await _browser.GoToPage(url);
        message.Content = result ? $"Page {url} is open." : $"Page {url} open failed.";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return result;
    }
}
