namespace BotSharp.Plugin.WebDriver.Functions;

public class ChangeCheckboxFn : IFunctionCallback
{
    public string Name => "change_checkbox";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ChangeCheckboxFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        var result = await _browser.ChangeCheckbox(new BrowserActionParams(agent, args, convService.ConversationId, message.MessageId));

        var content = $"{(args.UpdateValue == "check" ? "Check" : "Uncheck")} checkbox of '{args.ElementText}'";
        message.Content = result ? 
            $"{content} successfully" : 
            $"{content} failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(convService.ConversationId, path);

        return true;
    }
}
