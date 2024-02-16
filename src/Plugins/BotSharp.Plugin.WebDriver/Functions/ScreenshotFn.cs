namespace BotSharp.Plugin.WebDriver.Functions;

public class ScreenshotFn : IFunctionCallback
{
    public string Name => "take_screenshot";

    private readonly IServiceProvider _services;
    private readonly IWebBrowser _browser;

    public ScreenshotFn(IServiceProvider services,
        IWebBrowser browser)
    {
        _services = services;
        _browser = browser;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var convService = _services.GetRequiredService<IConversationService>();

        var webDriverService = _services.GetRequiredService<WebDriverService>();
        var path = webDriverService.GetScreenshotFilePath(message.MessageId);

        message.Data = await _browser.ScreenshotAsync(convService.ConversationId, path);
        message.Content = "Took screenshot completed. You can take another screenshot if needed.";

        return true;
    }
}
