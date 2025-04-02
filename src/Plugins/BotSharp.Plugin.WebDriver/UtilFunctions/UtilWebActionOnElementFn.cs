namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebActionOnElementFn : IFunctionCallback
{
    public string Name => "util-web-action_on_element";
    public string Indication => "Do action on element.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebActionOnElementFn(
        IServiceProvider services,
        ILogger<UtilWebActionOnElementFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var locatorArgs = JsonSerializer.Deserialize<ElementLocatingArgs>(message.FunctionArgs);
        var actionArgs = JsonSerializer.Deserialize<ElementActionArgs>(message.FunctionArgs);
        if (actionArgs.Action == BroswerActionEnum.InputText)
        {
            // Replace variable in input text
            if (actionArgs.Content.StartsWith("@"))
            {
                var config = _services.GetRequiredService<IConfiguration>();
                var key = actionArgs.Content.Replace("@", string.Empty);
                actionArgs.Content = key.Replace(key, config[key]);
            }
        }

        actionArgs.WaitTime = actionArgs.WaitTime > 0 ? actionArgs.WaitTime : 2;

        var conv = _services.GetRequiredService<IConversationService>();

        var services = _services.CreateScope().ServiceProvider;
        var browser = services.GetRequiredService<IWebBrowser>();
        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var msg = new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            ContextId = webDriverService.GetMessageContext(message),
        };
        var result = await browser.ActionOnElement(msg, locatorArgs, actionArgs);

        message.Content = $"{actionArgs.Action} executed {(result.IsSuccess ? "success" : "failed")}.";

        // Add Current Url info to the message
        if (actionArgs.ShowCurrentUrl)
        {
            message.Content += $" Current page url: '{result.UrlAfterAction}'.";
        }

        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await browser.ScreenshotAsync(msg, path);

        return true;
    }
}
