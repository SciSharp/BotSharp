using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.Twilio.Services;
using StackExchange.Redis;

namespace BotSharp.Plugin.Twilio;

public class TwilioPlugin : IBotSharpPlugin
{
    public string Id => "943ffd4d-ac8b-44aa-8a1c-38c9279c1b65";
    public string Name => "Twilio";
    public string Description => "Communication APIs for SMS, Voice, Video & Authentication";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<TwilioSetting>("Twilio");
        });
        services.AddScoped<TwilioService>();
        var conn = ConnectionMultiplexer.Connect(config["Twilio:RedisConnectionString"]);
        var sessionManager = new TwilioSessionManager(conn);
        services.AddSingleton<ITwilioSessionManager>(sessionManager);
        services.AddSingleton<TwilioMessageQueue>();
        services.AddHostedService<TwilioMessageQueueService>();

    }
}
