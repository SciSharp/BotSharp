namespace BotSharp.Plugin.WebDriver.Functions;

public class HttpRequestFn : IFunctionCallback
{
    public string Name => "send_http_request";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public HttpRequestFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        var args = JsonSerializer.Deserialize<HttpRequestParams>(message.FunctionArgs);

        var agentService = _services.GetRequiredService<IAgentService>();
        var agent = await agentService.LoadAgent(message.CurrentAgentId);
        var result = await _browser.SendHttpRequest(new MessageInfo
        {
            AgentId = agent.Id,
            MessageId = message.MessageId,
            ContextId = convService.ConversationId
        }, args);

        message.Content = result.IsSuccess ? 
            result.Body :
            $"Http request failed. {result.Message}";

        return true;
    }
}
