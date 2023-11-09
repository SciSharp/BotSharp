using BotSharp.Abstraction.Plugins;
using BotSharp.Plugin.Twilio.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace BotSharp.Plugin.Twilio;

public class TwilioPlugin : IBotSharpPlugin
{
    public void RegisterDI(IServiceCollection services, IConfiguration config)
    {
        var setting = new TwilioSetting();
        config.Bind("Twilio", setting);
        services.AddSingleton(setting);
    }
}
