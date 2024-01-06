using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class OpenBrowserFn : IFunctionCallback
{
    public string Name => "open_browser";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public OpenBrowserFn(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        var browser = await _driver.LaunchBrowser(args.Url);
        message.Content = string.IsNullOrEmpty(args.Url) ? "Launch browser successfully." : $"Open website successfully.";
        message.Content += "\r\nWhat would you like to do next?";
        message.StopCompletion = true;
        return true;
    }
}
