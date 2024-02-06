namespace BotSharp.Plugin.WebDriver.Functions;

public class CheckRadioButtonFn : IFunctionCallback
{
    public string Name => "check_radio_button";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public CheckRadioButtonFn(IServiceProvider services,
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
        var result = await _browser.CheckRadioButton(new BrowserActionParams(agent, args, message.MessageId));

        message.Content = result ? 
            $"Checked value of '{args.UpdateValue}' for radio button '{args.ElementName}' successfully" : 
            "Failed";

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(path);

        return true;
    }
}
