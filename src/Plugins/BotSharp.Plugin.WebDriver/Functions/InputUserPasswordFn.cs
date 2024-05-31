namespace BotSharp.Plugin.WebDriver.Functions;

public class InputUserPasswordFn : IFunctionCallback
{
    public string Name => "input_user_password";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public InputUserPasswordFn(IServiceProvider services,
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

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        args.Password = webDriverService.ReplaceToken(args.Password);
        var result = await _browser.InputUserPassword(new BrowserActionParams(agent, args, convService.ConversationId, message.MessageId));

        message.Content = result.IsSuccess ? "Input password successfully" : "Input password failed";

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            ContextId = convService.ConversationId,
            MessageId = message.MessageId
        }, path);

        return true;
    }
}
