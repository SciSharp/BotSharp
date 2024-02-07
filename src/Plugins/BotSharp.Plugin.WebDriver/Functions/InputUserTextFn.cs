namespace BotSharp.Plugin.WebDriver.Functions;

public class InputUserTextFn : IFunctionCallback
{
    public string Name => "input_user_text";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public InputUserTextFn(IServiceProvider services,
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
        var result = await _browser.InputUserText(new BrowserActionParams(agent, args, message.MessageId));

        var content = $"Input '{args.InputText}' in element '{args.ElementText}'";
        if (args.PressEnter != null && args.PressEnter == true)
        {
            content += " and pressed Enter";
        }

        message.Content = result ?
            content + " successfully" : 
            "Failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
