namespace BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

public partial class PlaywrightWebDriver
{
    private readonly IServiceProvider _services;
    private readonly PlaywrightInstance _instance;
    public PlaywrightInstance Instance => _instance;

    public PlaywrightWebDriver(IServiceProvider services, PlaywrightInstance instance)
    {
        _services = services;
        _instance = instance;
    }
}
