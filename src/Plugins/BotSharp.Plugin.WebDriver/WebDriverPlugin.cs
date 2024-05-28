using BotSharp.Abstraction.Browsing.Settings;
using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.WebDriver.Drivers.PlaywrightDriver;
using BotSharp.Plugin.WebDriver.Drivers.SeleniumDriver;
using BotSharp.Plugin.WebDriver.Hooks;

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
        var settings = new WebBrowsingSettings();
        config.Bind("WebBrowsing", settings);

        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settings;
        });

        services.AddScoped<PlaywrightWebDriver>();
        services.AddSingleton<PlaywrightInstance>();

        services.AddScoped<SeleniumWebDriver>();
        services.AddSingleton<SeleniumInstance>();

        services.AddScoped<IWebBrowser>(provider => settings.Driver switch
        {
            "Playwright" => provider.GetRequiredService<PlaywrightWebDriver>(),
            "Selenium" => provider.GetRequiredService<SeleniumWebDriver>(),
            _ => provider.GetRequiredService<PlaywrightWebDriver>(),
        });

        services.AddScoped<WebDriverService>();
        services.AddScoped<IConversationHook, WebDriverConversationHook>();
    }
}
