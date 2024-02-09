namespace BotSharp.Plugin.WebDriver.Functions;

public class ClickElementFn : IFunctionCallback
{
    public string Name => "click_element";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ClickElementFn(IServiceProvider services,
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
        var result = await _browser.ClickElement(new BrowserActionParams(agent, args, message.MessageId));

        var content = $"Click element {args.MatchRule} text '{args.ElementText}'";
        message.Content = result ? 
            $"{content} successfully" : 
            $"{content} failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
