using BotSharp.Abstraction.Settings;
using BotSharp.Plugin.Twilio.Interfaces;
using BotSharp.Plugin.Twilio.OutboundPhoneCallHandler.Hooks;
using BotSharp.Plugin.Twilio.Services;
using StackExchange.Redis;
using Twilio;

namespace BotSharp.Plugin.Twilio;

public class TwilioPlugin : IBotSharpPlugin
{
    public string Id => "943ffd4d-ac8b-44aa-8a1c-38c9279c1b65";
    public string Name => "Twilio";
    public string Description => "Communication APIs for SMS, Voice, Video & Authentication";
    public string IconUrl => "https://w7.pngwing.com/pngs/918/671/png-transparent-twilio-full-logo-tech-companies.png";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        services.AddScoped(provider =>
        {
            var settingService = provider.GetRequiredService<ISettingService>();
            return settingService.Bind<TwilioSetting>("Twilio");
        });
        TwilioClient.Init(config["Twilio:AccountSid"], config["Twilio:RequestValidation:AuthToken"]);
        services.AddScoped<TwilioService>();

        var conn = ConnectionMultiplexer.Connect(config["Database:Redis"]);
        var sessionManager = new TwilioSessionManager(conn);

        services.AddSingleton<ITwilioSessionManager>(sessionManager);
        services.AddSingleton<TwilioMessageQueue>();
        services.AddHostedService<TwilioMessageQueueService>();
        services.AddTwilioRequestValidation();
        services.AddScoped<IAgentUtilityHook, OutboundPhoneCallHandlerUtilityHook>();
    }
}
