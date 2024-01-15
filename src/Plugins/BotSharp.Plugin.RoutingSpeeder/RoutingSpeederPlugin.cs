using BotSharp.Abstraction.Conversations;
using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.RoutingSpeeder.Settings;
using BotSharp.Plugin.RoutingSpeeder.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BotSharp.Abstraction.Settings;

namespace BotSharp.Plugin.RoutingSpeeder;

public class RoutingSpeederPlugin : IBotSharpPlugin
{
    public string Id => "e7dff028-462d-47d2-85aa-dc56a6d362ee";
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<RouterSpeederSettings>("RouterSpeeder");
        });

        services.AddSingleton<ClassifierSetting>();

        services.AddScoped<IConversationHook, RoutingConversationHook>();
        services.AddSingleton<IntentClassifier>();
    }
}
