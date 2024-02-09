namespace BotSharp.Plugin.WebDriver.Functions;

public class ChangeListValueFn : IFunctionCallback
{
    public string Name => "change_list_value";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ChangeListValueFn(IServiceProvider services,
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
        var result = await _browser.ChangeListValue(new BrowserActionParams(agent, args, message.MessageId));

        var content = $"Change value to '{args.UpdateValue}' for {args.ElementName}";
        message.Content = result ? 
            $"{content} successfully" : 
            $"{content} failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
