using BotSharp.Abstraction.Plugins;
using BotSharp.Abstraction.Settings;
using BotSharp.Core.SideCar.Services;
using Microsoft.Extensions.Configuration;

namespace BotSharp.Core.SideCar;

public class BotSharpSideCarPlugin : IBotSharpPlugin
{
    public string Id => "06e5a276-bba0-45af-9625-889267c341c9";
    public string Name => "Side Car";
    public string Description => "Provides side car for calling agent cluster in conversation";

    public SettingsMeta Settings => new SettingsMeta("SideCar");
    public object GetNewSettingsInstance() => new SideCarSettings();

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var settings = new SideCarSettings();
        config.Bind("SideCar", settings);
        services.AddSingleton(settings);

        if (settings.Conversation.Provider == "botsharp")
        {
            services.AddScoped<IConversationSideCar, BotSharpConversationSideCar>();
        }
    }
}
