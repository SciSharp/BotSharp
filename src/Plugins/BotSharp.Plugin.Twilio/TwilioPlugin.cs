using BotSharp.Plugin.Twilio.Services;

namespace BotSharp.Plugin.Twilio;

public class TwilioPlugin : IBotSharpPlugin
{
    public string Id => "943ffd4d-ac8b-44aa-8a1c-38c9279c1b65";
    public string Name => "Twilio";
    public string Description => "Communication APIs for SMS, Voice, Video & Authentication";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var setting = new TwilioSetting();
        config.Bind("Twilio", setting);
        services.AddSingleton(setting);

        services.AddScoped<TwilioService>();
    }
}
