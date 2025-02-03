namespace BotSharp.Plugin.WebDriver.UtilFunctions;

public class UtilWebGoToPageFn : IFunctionCallback
{
    public string Name => "util-web-go_to_page";
    public string Indication => "Open web page.";
    private readonly IServiceProvider _services;
    private readonly ILogger _logger;

    public UtilWebGoToPageFn(
        IServiceProvider services,
        ILogger<UtilWebGoToPageFn> logger)
    {
        _services = services;
        _logger = logger;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<PageActionArgs>(message.FunctionArgs, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        args.WaitForNetworkIdle = false;
        args.WaitTime = 5;
        args.OpenNewTab = true;

        var conv = _services.GetRequiredService<IConversationService>();

        var browser = _services.GetRequiredService<IWebBrowser>();
        var msg = new MessageInfo
        {
            AgentId = message.CurrentAgentId,
            MessageId = message.MessageId,
            ContextId = message.CurrentAgentId,
        };
        await browser.CloseCurrentPage(msg);
        await browser.GoToPage(msg, args);

        message.Content = $"Open web page successfully.";

        return true;
    }
}
