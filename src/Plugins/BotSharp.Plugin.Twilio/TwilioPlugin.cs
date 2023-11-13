using BotSharp.Plugin.Twilio.Settings;

namespace BotSharp.Plugin.Twilio;

public class TwilioPlugin : IBotSharpPlugin
{
    public string Name => "Twilio";
    public string Description => "Communication APIs for SMS, Voice, Video & Authentication";

    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var setting = new TwilioSetting();
        config.Bind("Twilio", setting);
        services.AddSingleton(setting);
    }
}
