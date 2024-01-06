using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;
using BotSharp.Plugin.WebDriver.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Plugin.Playwrights;

public class WebDriverPlugin : IBotSharpPlugin
{
    public string Name => "Web Driver";
    public string Description => "Empower agent to manipulate web browser in automation tools.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/8576/8576378.png";
    public bool WithAgent => true;

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<PlaywrightWebDriver>();
        services.AddSingleton<PlaywrightInstance>();
        services.AddScoped<WebDriverService>();
    }
}
