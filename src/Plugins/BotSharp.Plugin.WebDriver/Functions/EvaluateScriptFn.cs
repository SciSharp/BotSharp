namespace BotSharp.Plugin.WebDriver.Functions;

public class EvaluateScriptFn : IFunctionCallback
{
    public string Name => "evaluate_script";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public EvaluateScriptFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();
        message.Data = await _browser.EvaluateScript<object>(convService.ConversationId, message.Content);
        return true;
    }
}
