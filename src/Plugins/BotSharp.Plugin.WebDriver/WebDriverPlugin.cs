using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;

namespace BotSharp.Plugin.Playwrights;

public class WebDriverPlugin : IBotSharpPlugin
{
    public string Id => "f0e26bc3-cfde-4b63-845c-c9c542abea44";
    public string Name => "Web Driver";
    public string Description => "Empower agent to manipulate web browser in automation tools.";
    public string IconUrl => "https://cdn-icons-png.flaticon.com/512/8576/8576378.png";
    public string[] AgentIds => new[] { "f3ae2a0f-e6ba-4ee1-a0b9-75d7431ff32b" };

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped<IWebBrowser, PlaywrightWebDriver>();
        services.AddSingleton<PlaywrightInstance>();
        services.AddScoped<WebDriverService>();
    }
}
