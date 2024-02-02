using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.WebDriver.Functions;

public class SwitchToNewTab : IFunctionCallback
{
    public string Name => "switch_to_new_tab";

    private readonly IServiceProvider _services;
    private readonly PlaywrightWebDriver _driver;

    public SwitchToNewTab(IServiceProvider services,
        PlaywrightWebDriver driver)
    {
        _services = services;
        _driver = driver;
    }

    public async Task<bool> Execute(RoleDialogModel message)
    {
        var args = JsonSerializer.Deserialize<BrowsingContextIn>(message.FunctionArgs);
        await _driver.SwitchToNewTab();
        message.Content = "Switched to new tab page";
        return true;
    }
}
