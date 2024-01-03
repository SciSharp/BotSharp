using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;
using BotSharp.Plugin.WebDriver.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.Playwrights;

public class WebDriverPlugin : IBotSharpPlugin
{
    public string Name => "Web Driver";
    public string Description => "Manipulate web browser in automation tools.";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<PlaywrightWebDriver>();
        services.AddSingleton<PlaywrightInstance>();
        services.AddScoped<WebDriverService>();
    }
}
